using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using TikBot.Models;
using TikBot.Handlers.ProgressGroup;
using TikBot.Handlers.UpgradeGroup;
using Message = Telegram.Bot.Types.Message;
using TikBot.Handlers.PrivateChat;
using System.Windows.Forms;


namespace TikBot.Services
{
    public class MessageService
    {
        private readonly UserService _userService;
        private readonly GroupService _groupService;
        private readonly TicketService _ticketService;
        private readonly PollService _pollService;
        private readonly GroupInfoService _groupInfoService;
        private readonly StudentService _studentService;
        private readonly InviteService _inviteService;
        private readonly ReportService _reportService;
        private readonly MentorService _mentorService;

        public MessageService(
            UserService userService,
            GroupService groupService,
            TicketService ticketService,
            PollService pollService,
            GroupInfoService groupInfoService,
            StudentService studentService,
            InviteService inviteService,
            ReportService reportService,
            MentorService mentorService)
        {
            _userService = userService;
            _groupService = groupService;
            _ticketService = ticketService;
            _pollService = pollService;
            _groupInfoService = groupInfoService;
            _studentService = studentService;
            _inviteService = inviteService;
            _reportService = reportService;
            _mentorService = mentorService;
        }

        public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message.Text == null || message.From == null)
                return;

            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            var userRole = _userService.GetUserRole(userId, chatId);

            // Register group info and student info for students' messages in progress groups
            if (userRole == UserRole.Student && (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup))
            {
                var groupType = _groupService.GetGroupType(chatId);
                if (groupType == GroupType.Progress)
                {
                    var groupName = message.Chat.Title ?? "Unknown Group";
                    var groupLink = await GetGroupLinkAsync(botClient, chatId, cancellationToken) ?? "No Link";
                    _groupInfoService.RegisterGroupInfo(chatId, groupName, groupLink);
                    await _studentService.RegisterStudentAsync(botClient, message, cancellationToken);
                }
            }

            // Check for pending actions (report, complaint, feedback, appreciation, other)
            if (message.Chat.Type == ChatType.Private && userRole == UserRole.Student)
            {
                var pendingComplaintId = _studentService.GetPendingActionMessageId(userId, "Complaint");
                if (_studentService.IsPendingAction(userId, "Complaint", pendingComplaintId))
                {
                    await new ComplaintHandler(_studentService, _userService).HandleResponseAsync(botClient, message, cancellationToken);
                    return;
                }

                var pendingFeedbackId = _studentService.GetPendingActionMessageId(userId, "Feedback");
                if (_studentService.IsPendingAction(userId, "Feedback", pendingFeedbackId))
                {
                    await new FeedbackHandler(_studentService, _userService).HandleResponseAsync(botClient, message, cancellationToken);
                    return;
                }

                var pendingAppreciationId = _studentService.GetPendingActionMessageId(userId, "Appreciation");
                if (_studentService.IsPendingAction(userId, "Appreciation", pendingAppreciationId))
                {
                    await new AppreciationHandler(_studentService, _userService).HandleResponseAsync(botClient, message, cancellationToken);
                    return;
                }

                var pendingContactOtherId = _studentService.GetPendingActionMessageId(userId, "ContactOther");
                if (_studentService.IsPendingAction(userId, "ContactOther", pendingContactOtherId))
                {
                    await HandleOtherResponseAsync(botClient, message, "Contact", cancellationToken);
                    return;
                }

                var pendingProgramOtherId = _studentService.GetPendingActionMessageId(userId, "ProgramOther");
                if (_studentService.IsPendingAction(userId, "ProgramOther", pendingProgramOtherId))
                {
                    await HandleOtherResponseAsync(botClient, message, "Program", cancellationToken);
                    return;
                }
            }

            if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
            {
                var groupType = _groupService.GetGroupType(chatId);
                if (groupType == GroupType.Progress && userRole == UserRole.Mentor)
                {
                    var pendingMessageId = _reportService.GetPendingMessageId(userId, chatId);
                    if (_reportService.IsPendingReport(userId, chatId, pendingMessageId))
                    {
                        await new ReportHandler(_reportService, _userService).HandleReportReasonAsync(botClient, message, cancellationToken);
                        return;
                    }
                }
            }

            if (message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                if (message.Chat.Type == ChatType.Private)
                {
                    await HandlePrivateStartAsync(botClient, message, userRole, cancellationToken);
                }
                else if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
                {
                    await HandleGroupStartAsync(botClient, message, userRole, cancellationToken);
                }
            }
            else if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
            {
                var groupType = _groupService.GetGroupType(chatId);
                if (groupType == GroupType.Progress)
                {
                    var isButton = new[]
                    {
                        "تماس گرفته شد",
                        "گزارش بررسی شد",
                        "تیکت به دانش‌آموز",
                        "برنامه ارسال شد",
                        "ریپورت",
                        "دعوت از دکتر احمدی",
                        "گزارش ارسال نشده"
                    }.Contains(message.Text);

                    if (isButton)
                    {
                        await HandleProgressGroupButtonsAsync(botClient, message, userRole, cancellationToken);
                    }
                }
            }
            else if (message.Chat.Type == ChatType.Private)
            {
                await HandlePrivateButtonsAsync(botClient, message, userRole, cancellationToken);
            }
        }

        private async Task HandleOtherResponseAsync(ITelegramBotClient botClient, Message message, string type, CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;
            var action = type == "Contact" ? "ContactOther" : "ProgramOther";
            var pendingMessageId = _studentService.GetPendingActionMessageId(userId, action);

            if (!_studentService.IsPendingAction(userId, action, pendingMessageId))
                return;

            var student = _studentService.GetStudentInfo(userId);
            if (student == null || student.GroupId == 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "شما در گروهی ثبت‌نام نکردید. لطفاً در گروه پیشرفت پیام دهید.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var (groupName, groupLink) = await GetGroupInfoAsync(botClient, student.GroupId, cancellationToken);
            var mentorId = student.MentorId;
            var mentorName = _userService.GetMentor(mentorId)?.FullName ?? "منتور ناشناس";
            var mentorLink = $"tg://user?id={mentorId}";

            var score = _studentService.GetPendingActionMessageId(userId, action + "_Score");
            var scoreValue = score >= 1 && score <= 10 ? score : 5;

            string topic, alertText;
            long targetChatId;

            if (scoreValue >= 1 && scoreValue <= 4)
            {
                topic = $"شکایت {type.ToLower()}";
                alertText = "از نارضایتی شما خیلی متأسفیم\nنظر شما مستقیماً برای دکتر احمدی ارسال و پیگیری می‌شه";
                targetChatId = -4652850967;
            }
            else if (scoreValue >= 5 && scoreValue <= 7)
            {
                topic = $"نقد {type.ToLower()}";
                alertText = "مرسی که با نظرت به بهبود ما کمک کردی";
                targetChatId = -4786584682;
            }
            else
            {
                topic = $"قدردانی {type.ToLower()}";
                alertText = "خوشحالیم که راضی بودی\nممنون از همکاریت";
                targetChatId = -4678544514;
            }

            var feedbackMessage = $"نام گروه: [{groupName}]({groupLink})\n" +
                                 $"نام منتور: [{mentorName}]({mentorLink})\n" +
                                 $"موضوع: {topic}\n" +
                                 $"دلیل: {message.Text ?? "بدون توضیح"}";

            await botClient.SendTextMessageAsync(
                chatId: targetChatId,
                text: feedbackMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: alertText,
                cancellationToken: cancellationToken
            );

            _studentService.CompletePendingAction(userId, action, pendingMessageId);
            _studentService.CompletePendingAction(userId, action + "_Score", score);
        }

        private async Task<(string GroupName, string GroupLink)> GetGroupInfoAsync(ITelegramBotClient botClient, long groupId, CancellationToken cancellationToken)
        {
            try
            {
                var chat = await botClient.GetChatAsync(groupId, cancellationToken);
                var groupName = chat.Title ?? "گروه بدون نام";
                var groupLink = chat.InviteLink ?? await botClient.ExportChatInviteLinkAsync(groupId, cancellationToken);
                return (groupName, groupLink);
            }
            catch
            {
                return ("گروه بدون نام", "لینک نامشخص");
            }
        }

        private async Task<string> GetGroupLinkAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var chat = await botClient.GetChatAsync(chatId, cancellationToken);
                return chat.InviteLink ?? await botClient.ExportChatInviteLinkAsync(chatId, cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        private async Task HandlePrivateStartAsync(ITelegramBotClient botClient, Message message, UserRole userRole, CancellationToken cancellationToken)
        {
            if (userRole == UserRole.Student)
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("شکایت از خدمات"), new KeyboardButton("قدردانی از خدمات") },
                    new[] { new KeyboardButton("تیکت‌های من"), new KeyboardButton("نقد و پیشنهاد") },
                    new[] { new KeyboardButton("ارتباط با دکتر احمدی") }
                })
                {
                    ResizeKeyboard = true
                };

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "سلام دوست عزیز👋\nبه ربات مشاوره تیک خوش اومدی!\nدکمه‌های من همیشه در دسترس تو هستن تا بتونیم باهم بهتر کار کنیم.",
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
            else if (userRole == UserRole.Mentor)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "منتور عزیز\nربات در پیوی فقط برای دانش‌آموزان فعال هست",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "شما عضو مجموعه تیک نیستید",
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task HandlePrivateButtonsAsync(ITelegramBotClient botClient, Message message, UserRole userRole, CancellationToken cancellationToken)
        {
            if (userRole != UserRole.Student)
            {
                var warningText = userRole == UserRole.Mentor
                    ? "منتور عزیز\nربات در پیوی فقط برای دانش‌آموزان فعال هست"
                    : "شما عضو مجموعه تیک نیستید";

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: warningText,
                    cancellationToken: cancellationToken
                );
                return;
            }

            switch (message.Text)
            {
                case "شکایت از خدمات":
                    await new ComplaintHandler(_studentService, _userService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "قدردانی از خدمات":
                    await new AppreciationHandler(_studentService, _userService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "تیکت‌های من":
                    await new MyTicketsHandler(_ticketService, _groupInfoService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "نقد و پیشنهاد":
                    await new FeedbackHandler(_studentService, _userService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "ارتباط با دکتر احمدی":
                    await new DrAhmadiHandler().HandleAsync(botClient, message, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "لطفاً از دکمه‌های موجود استفاده کنید.",
                        cancellationToken: cancellationToken
                    );
                    break;
            }
        }

        private async Task HandleGroupStartAsync(ITelegramBotClient botClient, Message message, UserRole userRole, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var groupType = _groupService.GetGroupType(chatId);
            string startMessage;

            if (groupType == GroupType.Progress)
            {
                startMessage = "سلام به گروه پیشرفت خوش اومدید!\nاینجا برای هماهنگی و پیشرفت دانش‌آموزان طراحی شده.";
                if (userRole == UserRole.Mentor)
                {
                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("تماس گرفته شد"), new KeyboardButton("گزارش بررسی شد") },
                        new[] { new KeyboardButton("تیکت به دانش‌آموز"), new KeyboardButton("برنامه ارسال شد") },
                        new[] { new KeyboardButton("ریپورت"), new KeyboardButton("دعوت از دکتر احمدی") },
                        new[] { new KeyboardButton("گزارش ارسال نشده") }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: startMessage,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: startMessage,
                        cancellationToken: cancellationToken
                    );
                }
            }
            else if (groupType == GroupType.Complaint || groupType == GroupType.Feedback || groupType == GroupType.Appreciation)
            {
                var groupName = groupType switch
                {
                    GroupType.Complaint => "شکایت خدمات",
                    GroupType.Feedback => "نقد و پیشنهاد",
                    GroupType.Appreciation => "قدردانی از منتور",
                    _ => "نامشخص"
                };

                startMessage = $"گروه {groupName}\nاین گروه کیبورد ندارد";
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: startMessage,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                startMessage = "سلام به گروه ارتقا خوش اومدید!\nاینجا برای منتورها و ناظرین طراحی شده.";
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("ظرفیت", "capacity"),
                        InlineKeyboardButton.WithCallbackData("امتیاز من", "my_score"),
                        InlineKeyboardButton.WithCallbackData("مرخصی", "leave")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("نظارت", "supervision")
                    }
                });

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: startMessage,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task HandleProgressGroupButtonsAsync(ITelegramBotClient botClient, Message message, UserRole userRole, CancellationToken cancellationToken)
        {
            if (userRole == UserRole.Student)
            {
                await botClient.DeleteMessageAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    cancellationToken: cancellationToken
                );

                var warningMessage = await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "دانش‌آموز عزیز، شما نمی‌تونید از دکمه‌های کیبورد استفاده کنید.",
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000, cancellationToken);
                await botClient.DeleteMessageAsync(
                    chatId: message.Chat.Id,
                    messageId: warningMessage.MessageId,
                    cancellationToken: cancellationToken
                );
                return;
            }

            switch (message.Text)
            {
                case "تماس گرفته شد":
                    await new ContactedHandler(_studentService, _userService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "گزارش بررسی شد":
                    await new ReportReviewedHandler(_ticketService, _pollService, _userService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "تیکت به دانش‌آموز":
                    await new TicketToStudentHandler(_ticketService, _studentService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "برنامه ارسال شد":
                    await new ProgramSentHandler(_studentService, _userService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "ریپورت":
                    await new ReportHandler(_reportService, _userService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "دعوت از دکتر احمدی":
                    await new InviteDrAhmadiHandler(_inviteService, _userService).HandleAsync(botClient, message, cancellationToken);
                    break;
                case "گزارش ارسال نشده":
                    await new ReportNotSentHandler(_pollService, _studentService).HandleAsync(botClient, message, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "لطفاً از دکمه‌های موجود استفاده کنید.",
                        cancellationToken: cancellationToken
                    );
                    break;
            }
        }

        public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var userId = callbackQuery.From.Id;
            var userRole = _userService.GetUserRole(userId, callbackQuery.Message.Chat.Id);
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var data = callbackQuery.Data;

            if (data.StartsWith("contact_score_") || data.StartsWith("program_score_"))
            {
                await HandleScoreCallbackAsync(botClient, callbackQuery, userRole, cancellationToken);
                return;
            }

            if (data.StartsWith("contact_reason_") || data.StartsWith("program_reason_"))
            {
                await HandleReasonCallbackAsync(botClient, callbackQuery, userRole, cancellationToken);
                return;
            }

            if (data.StartsWith("poll_"))
            {
                await HandlePollResponseAsync(botClient, callbackQuery, userRole, cancellationToken);
                return;
            }

            if (data.StartsWith("report_"))
            {
                await new ReportHandler(_reportService, _userService).HandleCallbackAsync(botClient, callbackQuery, cancellationToken);
                return;
            }
if (data.StartsWith("capacity_mentor_"))
            {
                await new CapacityHandler(_mentorService, _studentService, _userService)
                    .HandleMentorCapacityResponseAsync(botClient, callbackQuery, cancellationToken);
                return;
            }

            if (data.StartsWith("capacity_supervisor_"))
            {
                await new CapacityHandler(_mentorService, _studentService, _userService)
                    .HandleSupervisorCapacityResponseAsync(botClient, callbackQuery, cancellationToken);
                return;
            }

            switch (data)
            {
                case "capacity":
                    await new CapacityHandler(_mentorService, _studentService, _userService)
                        .HandleAsync(botClient, callbackQuery, cancellationToken);
                    break;
                case "my_score":
                    if (userRole == UserRole.Mentor)
                        await new MyScoreHandler().HandleAsync(botClient, callbackQuery, cancellationToken);
                    else
                        await botClient.AnswerCallbackQueryAsync(
                            callbackQueryId: callbackQuery.Id,
                            text: "این دکمه فقط برای منتورها فعال هست",
                            showAlert: true,
                            cancellationToken: cancellationToken
                        );
                    break;
                case "leave":
                    await new LeaveHandler(_mentorService, _studentService, _groupService, _userService)
                        .HandleAsync(botClient, callbackQuery, cancellationToken);
                    break;
                case "supervision":
                    if (userRole == UserRole.Supervisor)
                        await new SupervisionHandler().HandleAsync(botClient, callbackQuery, cancellationToken);
                    else
                        await botClient.AnswerCallbackQueryAsync(
                            callbackQueryId: callbackQuery.Id,
                            text: "این دکمه فقط برای ناظرین فعال هست",
                            showAlert: true,
                            cancellationToken: cancellationToken
                        );
                    break;
            }
        }

        private async Task HandleScoreCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, UserRole userRole, CancellationToken cancellationToken)
        {
            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var data = callbackQuery.Data;
            var type = data.StartsWith("contact_") ? "Contact" : "Program";
            var score = int.Parse(data.Split('_').Last());

            if (userRole != UserRole.Student)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "فقط دانش‌آموزان می‌تونن به نظرسنجی نمره بدن",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
                return;
            }

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"شما نمره {score} انتخاب کردید",
                showAlert: true,
                cancellationToken: cancellationToken
            );

            var student = _studentService.GetStudentInfo(userId);
            if (student == null || student.GroupId == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: "شما در گروهی ثبت‌نام نکردید. لطفاً در گروه پیشرفت پیام دهید.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            List<InlineKeyboardButton> reasons;
            string reasonText;

            if (score >= 1 && score <= 4)
            {
                reasonText = type == "Contact"
                    ? "دلیل نارضایتی از تماس امروزت/این هفته‌ت با منتور چیه؟"
                    : "دلیل نارضایتی از برنامه امروزت/این هفته‌ت با منتور چیه؟";
                reasons = type == "Contact"
                    ? new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("منتور با من تماس نگرفته", "contact_reason_no_contact"),
                        InlineKeyboardButton.WithCallbackData("در ساعت مناسب یا مقرر تماس گرفته نشد", "contact_reason_bad_timing"),
                        InlineKeyboardButton.WithCallbackData("مدت زمان تماس کوتاه بود", "contact_reason_short_duration"),
                        InlineKeyboardButton.WithCallbackData("محتوای تماس ارزشمند نبود", "contact_reason_low_value"),
                        InlineKeyboardButton.WithCallbackData("لحن و صحبت منتور محترمانه نبود", "contact_reason_bad_tone"),
                        InlineKeyboardButton.WithCallbackData("انرژی یا تمرکز منتور رضایت‌بخش نبود", "contact_reason_low_energy"),
                        InlineKeyboardButton.WithCallbackData("منتور به نظر یا شرایط من توجهی نداشت", "contact_reason_no_attention"),
                        InlineKeyboardButton.WithCallbackData("سایر", "contact_reason_other")
                    }
                    : new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("برنامه متناسب شرایط من نیست", "program_reason_not_personalized"),
                        InlineKeyboardButton.WithCallbackData("مطابق صحبت‌ها نیست", "program_reason_not_as_discussed"),
                        InlineKeyboardButton.WithCallbackData("برنامه دیر رسید", "program_reason_late"),
                        InlineKeyboardButton.WithCallbackData("تعداد دروس نامناسب", "program_reason_bad_volume"),
                        InlineKeyboardButton.WithCallbackData("تعادل برنامه مناسب نیست", "program_reason_bad_balance"),
                        InlineKeyboardButton.WithCallbackData("ظاهر نامناسب", "program_reason_bad_appearance"),
                        InlineKeyboardButton.WithCallbackData("توجه به کنکور و نهایی متعادل نیست", "program_reason_bad_exam_balance"),
                        InlineKeyboardButton.WithCallbackData("جزئیات ناقص", "program_reason_incomplete"),
                        InlineKeyboardButton.WithCallbackData("سایر", "program_reason_other")
                    };
            }
            else if (score >= 5 && score <= 7)
            {
                reasonText = type == "Contact"
                    ? "چه چیزی از تماس منتور می‌تونه بهتر بشه؟"
                    : "چه چیزی از برنامه منتور می‌تونه بهتر بشه؟";
                reasons = type == "Contact"
                    ? new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("ساعت برقراری تماس", "contact_reason_timing"),
                        InlineKeyboardButton.WithCallbackData("مدت زمان تماس", "contact_reason_duration"),
                        InlineKeyboardButton.WithCallbackData("ارزشمندی محتوای تماس", "contact_reason_value"),
                        InlineKeyboardButton.WithCallbackData("لحن و صحبت محترمانه منتور در تماس", "contact_reason_tone"),
                        InlineKeyboardButton.WithCallbackData("انرژی یا تمرکز منتور در طول تماس", "contact_reason_energy"),
                        InlineKeyboardButton.WithCallbackData("توجه منتور به نظر یا شرایط من", "contact_reason_attention"),
                        InlineKeyboardButton.WithCallbackData("سایر", "contact_reason_other")
                    }
                    : new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("شخصی‌سازی برنامه", "program_reason_personalization"),
                        InlineKeyboardButton.WithCallbackData("ارسال سریع‌تر", "program_reason_speed"),
                        InlineKeyboardButton.WithCallbackData("حجم برنامه", "program_reason_volume"),
                        InlineKeyboardButton.WithCallbackData("تعادل بیشتر", "program_reason_balance"),
                        InlineKeyboardButton.WithCallbackData("تعادل کنکور و نهایی", "program_reason_exam_balance"),
                        InlineKeyboardButton.WithCallbackData("ظاهر برنامه", "program_reason_appearance"),
                        InlineKeyboardButton.WithCallbackData("کامل‌تر بودن", "program_reason_completeness"),
                        InlineKeyboardButton.WithCallbackData("سایر", "program_reason_other")
                    };
            }
            else
            {
                reasonText = type == "Contact"
                    ? "از کدوم بخش تماست بیشتر رضایت داشتی؟"
                    : "از کدوم بخش برنامه بیشتر رضایت داشتی؟";
                reasons = type == "Contact"
                    ? new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("نظم و ساعت برقراری تماس", "contact_reason_good_timing"),
                        InlineKeyboardButton.WithCallbackData("مدت زمان کافی تماس", "contact_reason_good_duration"),
                        InlineKeyboardButton.WithCallbackData("کیفیت و ارزشمندی محتوای تماس", "contact_reason_high_value"),
                        InlineKeyboardButton.WithCallbackData("لحن و صحبت محترمانه و صمیمانه تماس", "contact_reason_good_tone"),
                        InlineKeyboardButton.WithCallbackData("انرژی و تمرکز بالای منتور", "contact_reason_high_energy"),
                        InlineKeyboardButton.WithCallbackData("توجه خوب به نظر یا شرایط من", "contact_reason_good_attention"),
                        InlineKeyboardButton.WithCallbackData("سایر", "contact_reason_other")
                    }
                    : new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("شخصی‌سازی دقیق", "program_reason_high_personalization"),
                        InlineKeyboardButton.WithCallbackData("ارسال به‌موقع", "program_reason_timely"),
                        InlineKeyboardButton.WithCallbackData("حجم مناسب", "program_reason_good_volume"),
                        InlineKeyboardButton.WithCallbackData("تعادل خوب", "program_reason_good_balance"),
                        InlineKeyboardButton.WithCallbackData("تعادل کنکور و نهایی", "program_reason_good_exam_balance"),
                        InlineKeyboardButton.WithCallbackData("ظاهر زیبا", "program_reason_good_appearance"),
                        InlineKeyboardButton.WithCallbackData("کامل بودن برنامه", "program_reason_complete"),
                        InlineKeyboardButton.WithCallbackData("سایر", "program_reason_other")
                    };
            }

            var keyboard = new InlineKeyboardMarkup(reasons.Select(r => new[] { r }).ToArray());

            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: reasonText,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );

            _studentService.StartPendingAction(userId, $"{type}Other_Score", score);
        }

        private async Task HandleReasonCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, UserRole userRole, CancellationToken cancellationToken)
        {
            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var data = callbackQuery.Data;
            var type = data.StartsWith("contact_") ? "Contact" : "Program";
            var reason = data.Split('_').Last();

            if (userRole != UserRole.Student)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "فقط دانش‌آموزان می‌تونن نظر بدن",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
                return;
            }

            var student = _studentService.GetStudentInfo(userId);
            if (student == null || student.GroupId == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: "شما در گروهی ثبت‌نام نکردید. لطفاً در گروه پیشرفت پیام دهید.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var (groupName, groupLink) = await GetGroupInfoAsync(botClient, student.GroupId, cancellationToken);
            var mentorId = student.MentorId;
            var mentorName = _userService.GetMentor(mentorId)?.FullName ?? "منتور ناشناس";
            var mentorLink = $"tg://user?id={mentorId}";

            var score = _studentService.GetPendingActionMessageId(userId, $"{type}Other_Score");
            var scoreValue = score >= 1 && score <= 10 ? score : 5;

            string topic, alertText, reasonText;
            long targetChatId;

            if (scoreValue >= 1 && scoreValue <= 4)
            {
                topic = $"شکایت {type.ToLower()}";
                alertText = "از نارضایتی شما خیلی متأسفیم\nنظر شما مستقیماً برای دکتر احمدی ارسال و پیگیری می‌شه";
                targetChatId = -4652850967;
            }
            else if (scoreValue >= 5 && scoreValue <= 7)
            {
                topic = $"نقد {type.ToLower()}";
                alertText = "مرسی که با نظرت به بهبود ما کمک کردی";
                targetChatId = -4786584682;
            }
            else
            {
                topic = $"قدردانی {type.ToLower()}";
                alertText = "خوشحالیم که راضی بودی\nممنون از همکاریت";
                targetChatId = -4678544514;
            }

            if (reason == "other")
            {
                reasonText = type == "Contact"
                    ? scoreValue <= 4 ? "دلیل نارضایتی از تماس امروزت/این هفته‌ت با منتور چیه؟"
                    : scoreValue <= 7 ? "چه چیزی از تماس منتور می‌تونه بهتر بشه؟"
                    : "از کدوم بخش تماست بیشتر رضایت داشتی؟"
                    : scoreValue <= 4 ? "دلیل نارضایتی از برنامه امروزت/این هفته‌ت با منتور چیه؟"
                    : scoreValue <= 7 ? "چه چیزی از برنامه منتور می‌تونه بهتر بشه؟"
                    : "از کدوم بخش برنامه بیشتر رضایت داشتی؟";

                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                var askMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: reasonText,
                    cancellationToken: cancellationToken
                );

                _studentService.StartPendingAction(userId, $"{type}Other", askMessage.MessageId);
                return;
            }

            reasonText = type == "Contact"
                ? reason switch
                {
                    "no_contact" => "منتور با من تماس نگرفته",
                    "bad_timing" => "در ساعت مناسب یا مقرر تماس گرفته نشد",
                    "short_duration" => "مدت زمان تماس کوتاه بود",
                    "low_value" => "محتوای تماس ارزشمند نبود",
                    "bad_tone" => "لحن و صحبت منتور محترمانه نبود",
                    "low_energy" => "انرژی یا تمرکز منتور رضایت‌بخش نبود",
                    "no_attention" => "منتور به نظر یا شرایط من توجهی نداشت",
                    "timing" => "ساعت برقراری تماس",
                    "duration" => "مدت زمان تماس",
                    "value" => "ارزشمندی محتوای تماس",
                    "tone" => "لحن و صحبت محترمانه منتور در تماس",
                    "energy" => "انرژی یا تمرکز منتور در طول تماس",
                    "attention" => "توجه منتور به نظر یا شرایط من",
                    "good_timing" => "نظم و ساعت برقراری تماس",
                    "good_duration" => "مدت زمان کافی تماس",
                    "high_value" => "کیفیت و ارزشمندی محتوای تماس",
                    "good_tone" => "لحن و صحبت محترمانه و صمیمانه تماس",
                    "high_energy" => "انرژی و تمرکز بالای منتور",
                    "good_attention" => "توجه خوب به نظر یا شرایط من",
                    _ => "نامشخص"
                }
                : reason switch
                {
                    "not_personalized" => "برنامه متناسب شرایط من نیست",
                    "not_as_discussed" => "مطابق صحبت‌ها نیست",
                    "late" => "برنامه دیر رسید",
                    "bad_volume" => "تعداد دروس نامناسب",
                    "bad_balance" => "تعادل برنامه مناسب نیست",
                    "bad_appearance" => "ظاهر نامناسب",
                    "bad_exam_balance" => "توجه به کنکور و نهایی متعادل نیست",
                    "incomplete" => "جزئیات ناقص",
                    "personalization" => "شخصی‌سازی برنامه",
                    "speed" => "ارسال سریع‌تر",
                    "volume" => "حجم برنامه",
                    "balance" => "تعادل بیشتر",
                    "exam_balance" => "تعادل کنکور و نهایی",
                    "appearance" => "ظاهر برنامه",
                    "completeness" => "کامل‌تر بودن",
                    "high_personalization" => "شخصی‌سازی دقیق",
                    "timely" => "ارسال به‌موقع",
                    "good_volume" => "حجم مناسب",
                    "good_balance" => "تعادل خوب",
                    "good_exam_balance" => "تعادل کنکور و نهایی",
                    "good_appearance" => "ظاهر زیبا",
                    "complete" => "کامل بودن برنامه",
                    _ => "نامشخص"
                };

            var feedbackMessage = $"نام گروه: [{groupName}]({groupLink})\n" +
                                 $"نام منتور: [{mentorName}]({mentorLink})\n" +
                                 $"موضوع: {topic}\n" +
                                 $"دلیل: {reasonText}";

            await botClient.SendTextMessageAsync(
                chatId: targetChatId,
                text: feedbackMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: alertText,
                showAlert: true,
                cancellationToken: cancellationToken
            );

            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
            _studentService.CompletePendingAction(userId, $"{type}Other_Score", score);
        }

        private async Task HandlePollResponseAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, UserRole userRole, CancellationToken cancellationToken)
        {
            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var score = int.Parse(callbackQuery.Data.Split('_')[1]);

            if (userRole != UserRole.Student)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "فقط دانش‌آموزان می‌تونن به نظرسنجی نمره بدن",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
                return;
            }

            var student = _studentService.GetStudentInfo(userId);
            if (student == null || student.GroupId == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: "شما در گروهی ثبت‌نام نکردید. لطفاً در گروه پیشرفت پیام دهید.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"شما نمره {score} انتخاب کردید\nممنون از همکاری شما",
                showAlert: true,
                cancellationToken: cancellationToken
            );

            // Record the poll score
            _pollService.RecordPollScore(userId, student.GroupId, score);

            await Task.Delay(5000, cancellationToken);
            await botClient.DeleteMessageAsync(
                chatId: chatId,
                messageId: messageId,
                cancellationToken: cancellationToken
            );
        }
    }
}
