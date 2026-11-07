// Renders docs\screenshots\settings.png and Assets\logo-64.png without showing anything on screen:
// the Settings window is drawn at an off-screen position, with default settings (not the user's file).
using System.Drawing.Imaging;
using System.Reflection;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var screenshots = Path.Combine(root, "docs", "screenshots");
Directory.CreateDirectory(screenshots);
ApplicationConfiguration.Initialize();

using (var icon = new Icon(Path.Combine(root, "Assets", "CastDesk.ico"), 64, 64))
using (var logo = icon.ToBitmap())
    logo.Save(Path.Combine(root, "Assets", "logo-64.png"), ImageFormat.Png);

var settingsFormType = typeof(CastDesk.Configuration.CastDeskOptions).Assembly.GetType("CastDesk.UI.SettingsForm")!;
var form = (Form)Activator.CreateInstance(settingsFormType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
    null, new object[] { new CastDesk.Configuration.CastDeskOptions(), false }, null)!;
form.StartPosition = FormStartPosition.Manual;
form.Location = new Point(-5000, -5000);
form.ShowInTaskbar = false;
form.Shown += (_, _) => form.BeginInvoke(() =>
{
    form.ActiveControl = null; // no selected text in the name box
    using var shot = new Bitmap(form.Width, form.Height);
    form.DrawToBitmap(shot, new Rectangle(Point.Empty, form.Size));
    shot.Save(Path.Combine(screenshots, "settings.png"), ImageFormat.Png);
    form.Close();
});
Application.Run(form);
