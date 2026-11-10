using System.Data;
using TabStripDemo.Controls;

namespace TabStripDemo;

internal static class SampleData
{
    public static IEnumerable<(TabItem Tab, DataTable Table)> Create()
    {
        yield return Make("디바이스", Color.FromArgb(0xD9, 0x41, 0x2B),
            new[] { ("보드 목록", "AM62x · i.MX8M · CV25"), ("연결 상태", "UART · SSH · ADB"), ("전원 제어", "릴레이 원격 리셋"), ("GPIO 모니터", "핀 상태 실시간 조회"), ("센서 보정", "IMX415 · AR0234") },
            new[] { "보드", "SoC", "연결", "펌웨어" },
            new[] { new[] { "EVB-01", "TI AM6254", "UART /dev/ttyUSB0", "v2.4.1" }, new[] { "CAM-07", "Ambarella CV25", "SSH 10.0.3.17", "v1.9.0" }, new[] { "GW-03", "NXP i.MX8M Plus", "끊김", "v3.0.0-rc2" } });

        yield return Make("펌웨어", Color.FromArgb(0xEC, 0x7A, 0x1C),
            new[] { ("이미지 관리", "bootloader · kernel · rootfs"), ("OTA 배포", "단계별 롤아웃"), ("서명 검증", "RSA-2048 · HAB"), ("버전 이력", "태그별 변경 내역"), ("복구 모드", "DFU · USB boot") },
            new[] { "이미지", "대상", "크기", "서명" },
            new[] { new[] { "u-boot.img", "AM62x", "1.2 MB", "검증됨" }, new[] { "Image.gz", "i.MX8M", "9.8 MB", "검증됨" }, new[] { "rootfs.ext4", "CV25", "212 MB", "대기" } });

        yield return Make("빌드", Color.FromArgb(0xE3, 0xA5, 0x14),
            new[] { ("Yocto 빌드", "scarthgap · kirkstone"), ("SDK 패키지", "크로스 툴체인"), ("빌드 큐", "대기 2 · 실행 1"), ("sstate 캐시", "공유 미러 관리"), ("아티팩트", "산출물 보관함") },
            new[] { "작업", "레시피", "상태", "소요" },
            new[] { new[] { "#1182", "core-image-minimal", "성공", "42분" }, new[] { "#1183", "bl-image-camera", "실행 중", "18분" }, new[] { "#1184", "meta-toolchain", "대기", "-" } });

        yield return Make("로그", Color.FromArgb(0x5F, 0x9E, 0x2C),
            new[] { ("콘솔 로그", "시리얼 실시간 출력"), ("커널 메시지", "dmesg 수집"), ("크래시 덤프", "core · ramoops"), ("필터 규칙", "태그 · 레벨"), ("내보내기", "CSV · 텍스트") },
            new[] { "시각", "보드", "레벨", "메시지" },
            new[] { new[] { "12:41:07", "CAM-07", "WARN", "isp: frame drop 3" }, new[] { "12:41:12", "EVB-01", "INFO", "eth0 link up 1000M" }, new[] { "12:42:30", "GW-03", "ERR", "pcie: link training failed" } });

        yield return Make("네트워크", Color.Empty,
            new[] { ("IP 설정", "DHCP · 고정 IP"), ("포트 포워딩", "SSH · RTSP"), ("VPN", "WireGuard 터널"), ("대역폭", "인터페이스별 사용량"), ("방화벽", "iptables 규칙") },
            new[] { "인터페이스", "주소", "상태", "속도" },
            new[] { new[] { "eth0", "10.0.3.17/24", "UP", "1000M" }, new[] { "wlan0", "-", "DOWN", "-" }, new[] { "wg0", "10.8.0.2/32", "UP", "-" } });

        yield return Make("설정", Color.Empty,
            new[] { ("일반", "언어 · 테마"), ("계정", "권한 · 토큰"), ("알림", "메일 · 슬랙"), ("단축키", "키 바인딩"), ("정보", "버전 1.4.2") },
            new[] { "항목", "값", "범위", "수정" },
            new[] { new[] { "테마", "시스템", "사용자", "오늘" }, new[] { "로그 보존", "30일", "전역", "11/02" }, new[] { "자동 재연결", "켜짐", "사용자", "10/28" } });
    }

    private static (TabItem, DataTable) Make(string text, Color accent, (string Title, string Caption)[] subs,
        string[] columns, string[][] rows)
    {
        var tab = new TabItem(text, accent);
        foreach (var (title, caption) in subs) tab.SubItems.Add(new SubItem(title, caption));

        var table = new DataTable(text);
        foreach (var c in columns) table.Columns.Add(c, typeof(string));
        foreach (var r in rows) table.Rows.Add(r.Cast<object>().ToArray());
        return (tab, table);
    }
}
