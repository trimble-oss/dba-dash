using DBADashGUI.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    /*
     InsightCard.ReleaseActiveControl - issue 2315.  The crash itself needs the Performance tab's own sequence of chart
     and card removals to make WinForms leave a container holding a disposed link, and doesn't reproduce in a test
     layout: these check what the method promises, so a card can be disposed without any container still holding it.
    */
    [TestClass]
    public class InsightCardFocusTests
    {
        /// <summary>The layout of the Performance tab: form > UserControl > SplitContainer panel > cards panel > cards.</summary>
        private sealed class Layout : System.IDisposable
        {
            public readonly Form Form = new() { Width = 600, Height = 400, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            public readonly UserControl View = new() { Dock = DockStyle.Fill };
            public readonly SplitContainer Split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            public readonly FlowLayoutPanel Cards = new() { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown };
            public readonly TextBox Other = new() { Dock = DockStyle.Fill };

            public Layout(int cardCount)
            {
                for (var i = 0; i < cardCount; i++)
                {
                    var card = new InsightCard { AutoSize = true };
                    card.Controls.Add(InsightCard.CreateContentLabel($"Card {i} with [a link](action:go{i})"));
                    Cards.Controls.Add(card);
                }
                Split.Panel1.Controls.Add(Cards);
                Split.Panel2.Controls.Add(Other);
                View.Controls.Add(Split);
                Form.Controls.Add(View);
                Form.Show();
                Pump();
            }

            public Control Card(int index) => Cards.Controls[index];

            public Control Link(int index) => Card(index).Controls[0];

            public void Dispose() => Form.Dispose();
        }

        [STATestMethod]
        public void ReleasesEveryContainerHoldingTheCardsLink()
        {
            using var layout = new Layout(2);
            var card = layout.Card(0);
            layout.Link(0).Select();
            Pump();
            Assert.AreSame(layout.Link(0), layout.Split.ActiveControl, "Precondition: the link is the split's active control.");
            Assert.AreSame(layout.Split, layout.View.ActiveControl, "Precondition: the split is the view's active control.");

            InsightCard.ReleaseActiveControl(card);

            AssertNoContainerHolds(layout.Form, card);

            layout.Cards.Controls.Remove(card);
            card.Dispose();
            Pump();
            AssertNoDisposedActiveControl(layout.Form);
        }

        [STATestMethod]
        public void LeavesFocusOutsideTheCardAlone()
        {
            using var layout = new Layout(2);
            layout.Link(0).Select();
            Pump();

            InsightCard.ReleaseActiveControl(layout.Card(1));

            Assert.AreSame(layout.Link(0), layout.Split.ActiveControl, "Another card's focused link should stay focused.");

            layout.Other.Select();
            Pump();

            InsightCard.ReleaseActiveControl(layout.Card(0));

            Assert.AreSame(layout.Other, layout.Split.ActiveControl, "A focused control outside the cards should stay focused.");
        }

        [STATestMethod]
        public void DoesNothingForACardWithoutAParent()
        {
            using var card = new InsightCard();
            card.Controls.Add(InsightCard.CreateContentLabel("[link](action:go)"));

            InsightCard.ReleaseActiveControl(card);
        }

        private static void AssertNoContainerHolds(Control top, Control card)
        {
            Control c = top;
            while (c is ContainerControl container && container.ActiveControl is { } active)
            {
                Assert.IsFalse(active == card || card.Contains(active),
                    $"{container.GetType().Name} still has {active.GetType().Name} from the card as its active control.");
                c = active;
            }
        }

        private static void AssertNoDisposedActiveControl(Form form)
        {
            Control c = form;
            while (c is ContainerControl container && container.ActiveControl is { } active)
            {
                Assert.IsFalse(active.IsDisposed, $"{container.GetType().Name} is left holding a disposed {active.GetType().Name}.");
                c = active;
            }
        }

        private static void Pump()
        {
            for (var i = 0; i < 10; i++)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
        }
    }
}
