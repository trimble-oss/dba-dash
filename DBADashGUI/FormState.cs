using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    internal class FormState
    {
        public int? Height { get; set; }
        public int? Width { get; set; }
        public Point? Location { get; set; }

        public FormWindowState? WindowState { get; set; }

        public static void TrackFormState(Form form, FormState state)
        {
            // Persist size changes
            form.SizeChanged += (s, e) =>
            {
                if (form.WindowState == FormWindowState.Minimized) return; // Don't persist minimized state
                state.WindowState = form.WindowState;
                state.Width = form.Width;
                state.Height = form.Height;
            };
            form.LocationChanged += (s, e) =>
            {
                if (form.WindowState == FormWindowState.Minimized) return; // Don't persist minimized state
                state.Location = form.Location;
            };
        }

        public static void ApplyFormState(Form form, FormState state)
        {
            if (state.Height.HasValue)
            {
                form.Height = state.Height.Value;
            }
            if (state.Width.HasValue)
            {
                form.Width = state.Width.Value;
            }
            if (state.WindowState.HasValue)
            {
                form.WindowState = state.WindowState.Value;
            }
            if (state.Location.HasValue)
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = state.Location.Value;
            }
        }
    }
}