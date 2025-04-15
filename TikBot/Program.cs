using TikBot.UI;

namespace TikBot
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new BoTik());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Program.Main: {ex.Message}", "Error");
            }
        }
    }
}