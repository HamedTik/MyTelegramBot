using TikBot.Core;
using TikBot.Services;
using TikBot.Utilities;
using System;
using System.Windows.Forms;

namespace TikBot.UI
{
    public partial class BoTik : Form
    {
        private TelegramBotService _botService;
        private readonly StatusManager _statusManager;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public BoTik()
        {
            try
            {
                MessageBox.Show("BoTik constructor started", "Debug");
                InitializeComponent();
                this.Text = "TikBot - Initializing...";
                this.WindowState = FormWindowState.Normal;
                this.Visible = true;
                this.BringToFront();
                MessageBox.Show("Form initialized", "Debug");

                _cancellationTokenSource = new CancellationTokenSource();
                _statusManager = new StatusManager(statusLabel);
                statusLabel.Text = "Status: Initializing";
                MessageBox.Show("StatusManager created", "Debug");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in BoTik constructor: {ex.Message}", "Error");
                throw;
            }
        }

        private async void BoTik_Load(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show("BoTik_Load started", "Debug");
                statusLabel.Text = "Status: Loading Services";

                // لود سرویس‌ها تو Background Thread
                await Task.Run(async () => await InitializeServicesAsync());
                MessageBox.Show("Services initialized", "Debug");

                if (_botService == null)
                {
                    statusLabel.Text = "Status: Error - BotService not initialized";
                    MessageBox.Show("BotService not initialized", "Error");
                    return;
                }

                statusLabel.Text = "Status: Starting Bot";
                MessageBox.Show("Starting TelegramBotService", "Debug");

                await _botService.StartAsync(_cancellationTokenSource.Token);
                statusLabel.Text = "Status: Online";
                this.Text = "TikBot - Online";
                MessageBox.Show("BoTik_Load completed", "Debug");
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Status: Error - {ex.Message}";
                MessageBox.Show($"Error in BoTik_Load: {ex.Message}", "Error");
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                MessageBox.Show("InitializeServicesAsync started", "Debug");

                if (string.IsNullOrEmpty(BotConfiguration.Token))
                {
                    Invoke((Action)(() => statusLabel.Text = "Status: Error - Missing Token"));
                    MessageBox.Show("Bot token is missing. Please set BotConfiguration.Token.", "Error");
                    return;
                }
                MessageBox.Show("Token checked", "Debug");

                var userService = new UserService();
                MessageBox.Show("UserService created", "Debug");

                var groupService = new GroupService();
                MessageBox.Show("GroupService created", "Debug");

                var groupInfoService = new GroupInfoService();
                MessageBox.Show("GroupInfoService created", "Debug");

                var mentorService = new MentorService();
                MessageBox.Show("MentorService created", "Debug");

                var studentService = new StudentService(userService, mentorService);
                MessageBox.Show("StudentService created", "Debug");

                var ticketService = new TicketService(studentService);
                MessageBox.Show("TicketService created", "Debug");

                var pollService = new PollService(studentService);
                MessageBox.Show("PollService created", "Debug");

                var inviteService = new InviteService();
                MessageBox.Show("InviteService created", "Debug");

                var reportService = new ReportService(userService);
                MessageBox.Show("ReportService created", "Debug");

                var messageService = new MessageService(
                    userService,
                    groupService,
                    ticketService,
                    pollService,
                    groupInfoService,
                    studentService,
                    inviteService,
                    reportService,
                    mentorService
                );
                MessageBox.Show("MessageService created", "Debug");

                _botService = new TelegramBotService(
                    BotConfiguration.Token,
                    userService,
                    groupService,
                    messageService,
                    _statusManager.UpdateStatus
                );
                MessageBox.Show("TelegramBotService created", "Debug");
            }
            catch (Exception ex)
            {
                Invoke((Action)(() => statusLabel.Text = $"Status: Error - {ex.Message}"));
                MessageBox.Show($"Error in InitializeServicesAsync: {ex.Message}", "Error");
                throw;
            }
        }

        private void BoTik_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                MessageBox.Show("BoTik_FormClosing started", "Debug");
                _botService?.Stop();
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                MessageBox.Show("BoTik_FormClosing completed", "Debug");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in BoTik_FormClosing: {ex.Message}", "Error");
            }
        }
    }
}
