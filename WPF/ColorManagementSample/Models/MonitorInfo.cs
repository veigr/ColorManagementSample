using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ColorManagementSample.Models
{
    /// <summary>
    /// モニタ情報
    /// </summary>
    internal class MonitorInfo
    {
        /// <summary>
        /// モニタプロファイル
        /// </summary>
        public ColorContext MonitorProfile { get; private set; }

        /// <summary>
        /// 範囲
        /// </summary>
        public Rect Bounds { get; private set; }

        /// <summary>
        /// 作業領域
        /// </summary>
        public Rect WorkingArea { get; private set; }

        /// <summary>
        /// モニタ名
        /// </summary>
        public string DeviceName { get; private set; }

        /// <summary>
        /// プライマリモニタかどうか
        /// </summary>
        public bool IsPrimary { get; private set; }

        /// <summary>
        /// 接続されているモニタを列挙
        /// </summary>
        public static IEnumerable<MonitorInfo> Monitors
        {
            get
            {
                var monitors = new List<MonitorInfo>();
                NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
                    {
                        monitors.Add(GetMonitorFromHDC(hMonitor));
                        return true;
                    }, IntPtr.Zero);
                return monitors;
            }
        }

        /// <summary>
        /// Rect中心点があるモニタを取得
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static MonitorInfo GetMonitorFromRect(Rect rect)
        {
            return GetMonitorFromPoint(new Point(
                    rect.Left + (rect.Right - rect.Left) / 2,
                    rect.Top + (rect.Bottom - rect.Top) / 2));
        }

        /// <summary>
        /// Pointが存在するモニタを取得
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static MonitorInfo GetMonitorFromPoint(Point point)
        {
            var hDC = NativeMethods.MonitorFromPoint(
                new POINT((int) point.X, (int) point.Y),
                MonitorOptions.MONITOR_DEFAULTTONEAREST);

            return GetMonitorFromHDC(hDC);
        }

        /// <summary>
        /// デバイスコンテキストからモニタを取得
        /// </summary>
        /// <param name="hDC"></param>
        /// <returns></returns>
        private static MonitorInfo GetMonitorFromHDC(IntPtr hDC)
        {
            var mi = new MONITORINFOEX();
            var success = NativeMethods.GetMonitorInfo(hDC, mi);
            if (!success) throw new InvalidOperationException();

            var dc = NativeMethods.CreateDC(mi.szDevice, mi.szDevice, null, IntPtr.Zero);

            var lpcbName = 0U;
            NativeMethods.GetICMProfile(dc, ref lpcbName, null);
            var sb = new StringBuilder((int) lpcbName);
            NativeMethods.GetICMProfile(dc, ref lpcbName, sb);

            var profileUri = sb.ToString();
            var context = new ColorContext(new Uri(profileUri));

            return new MonitorInfo
            {
                DeviceName = mi.szDevice,
                MonitorProfile = context,
                Bounds = new Rect(
                    mi.rcMonitor.Left,
                    mi.rcMonitor.Top,
                    mi.rcMonitor.Right - mi.rcMonitor.Left,
                    mi.rcMonitor.Bottom - mi.rcMonitor.Top),
                WorkingArea = new Rect(
                    mi.rcWork.Left,
                    mi.rcWork.Top,
                    mi.rcWork.Right - mi.rcWork.Left,
                    mi.rcWork.Bottom - mi.rcWork.Top),
                IsPrimary = mi.dwFlags == 1
            };
        }
    }

    internal static class UIElementExtensions
    {
        public static MonitorInfo GetCurrentMonitorInfo(this UIElement element)
        {
            var elementRect = new Rect(element.PointToScreen(new Point()),
                element.PointToScreen(new Point(element.RenderSize.Width, element.RenderSize.Height)));
            return MonitorInfo.GetMonitorFromRect(elementRect);
        }
    }
}
