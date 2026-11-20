#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBADash.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DBADash.Test
{
    /// <summary>Tests for <see cref="LogScope"/>: properties added to what's logged while a piece of work runs.</summary>
    [TestClass]
    public class LogScopeTests
    {
        private sealed class ListSink : ILogEventSink
        {
            public readonly List<LogEvent> Events = new();

            public void Emit(LogEvent logEvent)
            {
                lock (Events) Events.Add(logEvent);
            }
        }

        private static (ILogger Logger, ListSink Sink) CreateLogger()
        {
            var sink = new ListSink();
            var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
            return (logger, sink);
        }

        private static object Property(LogEvent e, string name) =>
            e.Properties.TryGetValue(name, out var value) ? ((ScalarValue)value).Value : null;

        [TestMethod]
        public async Task Connection_IsAdded_ToEventsOnTheAsyncFlow()
        {
            var (logger, sink) = CreateLogger();
            using (LogScope.Connection("SQL1"))
            {
                logger.Information("Before an await");
                await Task.Yield();
                await Task.Run(() => logger.Information("On another thread"));
            }
            logger.Information("After");

            CollectionAssert.AreEqual(new object[] { "SQL1", "SQL1", null },
                sink.Events.Select(e => Property(e, LogScope.ConnectionIDProperty)).ToArray());
        }

        [TestMethod]
        public void NothingToAdd_AddsNothing()
        {
            var (logger, sink) = CreateLogger();
            using (LogScope.Connection(null))
            using (LogScope.Connection(""))
            using (LogScope.Message(Guid.Empty))
            using (LogScope.Handle(Guid.Empty))
            {
                logger.Information("Nothing to add");
            }

            Assert.AreEqual(0, sink.Events.Single().Properties.Count);
        }

        [TestMethod]
        public void MessageAndHandle_AreAdded()
        {
            var (logger, sink) = CreateLogger();
            var id = Guid.NewGuid();
            var handle = Guid.NewGuid();
            using (LogScope.Handle(handle))
            using (LogScope.Message(id))
            {
                logger.Information("Processing");
            }

            var e = sink.Events.Single();
            Assert.AreEqual(id, Property(e, LogScope.MessageIdProperty));
            Assert.AreEqual(handle, Property(e, LogScope.HandleProperty));
        }

        [TestMethod]
        public void TheMessagesOwnProperty_Wins()
        {
            // A batch of connections logs each one's ID in its messages, inside a scope for another.
            var (logger, sink) = CreateLogger();
            using (LogScope.Connection("Outer"))
            {
                logger.Information("Collecting {ConnectionID}", "Inner");
            }

            Assert.AreEqual("Inner", Property(sink.Events.Single(), LogScope.ConnectionIDProperty));
        }

        /// <summary>
        /// Concurrent flows each keep their own ConnectionID through timed operations at a level that isn't logged.  An
        /// Operation.At(level) operation whose level is off is shared between callers, and gave flows each other's - see
        /// <see cref="LogTiming"/>.
        /// </summary>
        [TestMethod]
        public async Task TimedOperationsAtAnUnloggedLevel_KeepEachFlowsConnectionID()
        {
            var (logger, sink) = CreateLogger();
            var previous = Log.Logger;
            Log.Logger = logger; // Operations log through the static logger
            try
            {
                async Task Flow(string id)
                {
                    using var scope = LogScope.Connection(id);
                    for (var i = 0; i < 100; i++)
                    {
                        using (var op = LogTiming.Begin(LogEventLevel.Debug, "Outer {Id}", id))
                        {
                            await Task.Yield();
                            using (var inner = LogTiming.Begin(LogEventLevel.Debug, "Inner {Id}", id))
                            {
                                inner?.Complete();
                            }

                            op?.Complete();
                        }

                        Log.Information("Check {Expected}", id);
                    }
                }

                await Task.WhenAll(Enumerable.Range(0, 8).Select(n => Task.Run(() => Flow("SQL" + n))));
            }
            finally
            {
                Log.Logger = previous;
            }

            List<LogEvent> checks;
            lock (sink.Events) checks = sink.Events.Where(e => e.MessageTemplate.Text == "Check {Expected}").ToList();
            Assert.AreEqual(800, checks.Count);
            Assert.AreEqual(0, checks.Count(e => !Equals(Property(e, LogScope.ConnectionIDProperty), Property(e, "Expected"))),
                "every event carries its own flow's ConnectionID");
        }

        [TestMethod]
        public void ConnectionMessages_ExposeTheirConnectionID()
        {
            IConnectionMessage msg = new KillSessionMessage { ConnectionID = "SQL1" };
            Assert.AreEqual("SQL1", msg.ConnectionID);
        }
    }
}
