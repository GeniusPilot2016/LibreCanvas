using System.ComponentModel;

namespace Carpathia
{
    // Replacement for the OLD UserControl-generated designer file.
    // The previous InitializeComponent created an extra inner TabControl.
    // Leaving that code in place defeats native TabPage design-time editing.
    partial class TabControlWithCloseButtons
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }
    }
}
