using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace ADashboard.Properties
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [DebuggerNonUserCode]
    [CompilerGenerated]
    internal class CustomResources
    {
        private static ResourceManager resourceMan;
        private static CultureInfo resourceCulture;

        internal CustomResources()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (resourceMan == null)
                    resourceMan = new ResourceManager("ADashboard.Properties.Resources", typeof(CustomResources).Assembly);
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get => resourceCulture;
            set => resourceCulture = value;
        }

        internal static Bitmap bg => (Bitmap)ResourceManager.GetObject(nameof(bg), resourceCulture);

        internal static Bitmap bg_dash => (Bitmap)ResourceManager.GetObject("bg", resourceCulture);

        internal static Bitmap bg_new => (Bitmap)ResourceManager.GetObject(nameof(bg_new), resourceCulture);

        internal static Bitmap button_close => (Bitmap)ResourceManager.GetObject("button-close", resourceCulture);

        internal static Bitmap button_close_hover => (Bitmap)ResourceManager.GetObject("button-close-hover", resourceCulture);

        internal static Bitmap button_discord => (Bitmap)ResourceManager.GetObject("button-discord", resourceCulture);

        internal static Bitmap button_discord_hover => (Bitmap)ResourceManager.GetObject("button-discord-hover", resourceCulture);

        internal static Bitmap button_down => (Bitmap)ResourceManager.GetObject("button-down", resourceCulture);

        internal static Bitmap button_down_hover => (Bitmap)ResourceManager.GetObject("button-down-hover", resourceCulture);

        internal static Bitmap button_minimizer => (Bitmap)ResourceManager.GetObject("button-minimizer", resourceCulture);

        internal static Bitmap button_minimizer_hover => (Bitmap)ResourceManager.GetObject("button-minimizer-hover", resourceCulture);

        internal static Bitmap button_settings => (Bitmap)ResourceManager.GetObject("button-settings", resourceCulture);

        internal static Bitmap button_settings_hover => (Bitmap)ResourceManager.GetObject("button-settings-hover", resourceCulture);

        internal static Icon Cursor => (Icon)ResourceManager.GetObject(nameof(Cursor), resourceCulture);

        internal static Icon CursorTwo => (Icon)ResourceManager.GetObject(nameof(CursorTwo), resourceCulture);

        internal static Bitmap hand => (Bitmap)ResourceManager.GetObject(nameof(hand), resourceCulture);
    }
}
