using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Diagnostics;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            WPFUI.Background.Manager.Apply(WPFUI.Background.BackgroundType.Mica, this);

            bool chinese = Localization.IsChinese;
            Title = chinese ? "RoundedTB - 关于" : "RoundedTB - About";
            aboutTitleBar.Title = Title;
            okButton.Content = chinese ? "确定" : "OK";
            titleBlock.Text = chinese ? "欢迎使用 RoundedTB！" : "Welcome to RoundedTB!";
            bodyBlockMain.Inlines.Clear();
            bodyBlockMain.Inlines.Add(new Bold(new Run(chinese
                ? "感谢下载 RoundedTB。展开下方分类以查看更多信息。"
                : "Thanks for downloading RoundedTB. Expand the following categories for more information.")));
            expander0.Header = chinese ? "新增内容" : "What's new";
            expander1.Header = chinese ? "基本选项" : "About basic options";
            expander2.Header = chinese ? "高级选项" : "About advanced options";
            expander3.Header = chinese ? "已知问题" : "Known issues";
            expander4.Header = chinese ? "帮助和信息" : "Help and information";
            expander5.Header = chinese ? "调试" : "Debug";
            configButton.Content = chinese ? "打开配置文件" : "Open config file";
            logButton.Content = chinese ? "打开日志文件" : "Open log file";

            if (chinese)
            {
                SetPlainText(bodyBlock0,
                    "• 没有人知道。有人说这些构建版本里藏着秘密，还有一些 bug。说实话，我觉得其中 90% 都是 bug。95%。98%。实际上，你能看到这段文字本身可能就是一个 bug。呃，我放弃了。\n\n提醒：自动隐藏目前问题较多，必须在 Windows 中完全禁用。");
                SetPlainText(bodyBlock1,
                    "• 边距 - 按指定的逻辑像素在各边缩小任务栏。\n• 圆角半径 - 控制任务栏圆角大小。\n• 高级 - 打开高级和实验性功能菜单。\n• 应用 - 应用并保存尚未保存的更改。");
                SetPlainText(bodyBlock2,
                    "• [...] 边距 - 分别设置任务栏每一侧的边距。\n• 动态模式（Windows 11） - 根据打开的应用数量动态调整任务栏大小。\n• 分栏模式（Windows 10） - 允许手动调整任务栏大小，类似动态模式但需要额外设置。\n• 显示系统托盘 - 在动态/分栏模式中显示系统托盘，也可随时按 Win+F2 切换。\n• 悬停时显示系统托盘 - 鼠标悬停在系统托盘上时显示它，此选项优先于上面的设置。\n• TranslucentTB 兼容性 - 启用与 TranslucentTB 的兼容功能。\n• 最大化时填充任务栏 - 窗口最大化时填充所在显示器上的任务栏。\n• Alt+Tab 时填充任务栏（Windows 11） - 使用 Alt+Tab 或任务切换器时填充任务栏。");
                SetPlainText(bodyBlock3,
                    "• 在 Windows 中启用自动隐藏并使用 RoundedTB 可能导致严重闪烁、错误或无法访问任务栏，该功能不受支持。未来版本会提供正常工作的自定义自动隐藏。\n• 动态模式和分栏模式只有在任务栏位于显示器顶部或底部时才能正常工作。\n• Windows 10 的分栏模式目前仅支持主显示器。\n• 除 TranslucentTB 外的任务栏修改工具尚未正式支持；如果遇到兼容性问题，请反馈。\n• 使用动态模式时，任务栏偶尔可能过大、过小或不更新。将窗口移入/移出该显示器，或暂时更改任务栏对齐方式，通常可以修复这些问题。");
                SetPlainText(bodyBlock4,
                    "• 更详细的说明、提示和技巧请参阅 README。\n• 遇到问题或想交流 RoundedTB，欢迎加入官方 Discord 服务器。\n• 浏览源代码、提交问题或建议功能，请访问 RoundedTB GitHub 仓库。\n\nRoundedTB 使用 GNU 通用公共许可证 v3.0。使用本软件即表示你接受该许可证条款。");
            }
        }

        private static void SetPlainText(TextBlock block, string text)
        {
            block.Inlines.Clear();
            block.Text = text;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void configButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((MainWindow)Application.Current.MainWindow).configPath);
        }

        private void logButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((MainWindow)Application.Current.MainWindow).logPath);
        }
    }
}
