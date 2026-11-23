using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace VideoCapture
{
    public readonly struct GameEvent
    {
        public readonly float Time;
        public readonly string Id;
        public readonly string Detail;

        public GameEvent(float time, string id, string detail = "")
        {
            Time = time;
            Id = id;
            Detail = detail;
        }
    }

    /// <summary>Turns logged game events into draft .srt subtitles and a plain event list.</summary>
    public static class Captions
    {
        private const float MaxDuration = 3.5f;
        private const float GroupWindow = 1.5f; // events this close share one subtitle, so none flashes by
        private const float Gap = 0.05f;

        // {0} is the event's Detail.
        private static readonly Dictionary<string, (string ko, string en)> Lines = new()
        {
            ["start"] = ("Unity 2022.3에서 Unity 6로 마이그레이션", "Migrated from Unity 2022.3 to Unity 6"),
            ["camera"] = ("Cinemachine 3 카메라: 휠로 줌, 오른쪽 버튼 드래그로 회전", "Cinemachine 3 camera: wheel to zoom, right-drag to orbit"),
            ["walk"] = ("/ 키로 걷기와 달리기 전환", "/ toggles walking and running"),
            ["battle-pose"] = ("전투 중이 아닐 때 Z 키로 전투 자세 전환", "Z toggles the battle stance outside combat"),
            ["sprint"] = ("잰 발놀림(Q): 전투 중에는 지속 시간이 줄어드는 이동 속도 버프", "Quick Feet (Q): a speed buff that lasts less time in combat"),
            ["head-ik"] = ("왼쪽 클릭으로 대상 선택, 머리가 대상을 향함 (IK)", "Left-click to target; the head turns toward it (IK)"),
            ["cast-cancel"] = ("캐스팅 중에 이동하면 캐스팅 중단", "Moving during a cast cancels it"),
            ["first-battle"] = ("새로 추가한 중간 몬스터: 해골 전사", "New mid-level monsters: skeleton warriors"),
            ["off-gcd"] = ("MP 회복: 글로벌 쿨다운 중에도 사용할 수 있는 능력 (오프 GCD)", "MP restore: usable during the global cooldown (off-GCD)"),
            ["skeleton-down"] = ("해골 전사 처치 ({0})", "Skeleton warrior down ({0})"),
            ["target-switch"] = ("대상 전환: 바닥의 선택 표시가 새 대상으로 이동", "Target switch: the ground marker moves to the new target"),
            ["door-open"] = ("해골 전사를 모두 쓰러뜨리면 보스 방 문이 열림", "The boss door opens once both skeleton warriors fall"),
            ["feet-ik"] = ("울퉁불퉁한 지형에 맞춘 발 위치 보정 (IK)", "Feet adapt to uneven ground (IK)"),
            ["boss-engaged"] = ("보스전 시작, 음악이 보스 테마로 바뀜", "Boss fight: the music switches to the boss theme"),
            ["aoe-cast"] = ("보스 범위 공격: 캐스팅이 끝나기 전에 원 밖으로 나가면 회피", "Boss AOE: leave the circle before the cast ends to dodge"),
            ["aoe-dodged"] = ("회피 성공", "Dodged"),
            ["aoe-hit"] = ("범위 공격에 맞음", "Hit by the AOE"),
            ["boss-down"] = ("보스 처치", "Boss defeated"),
            ["cleared"] = ("게임 클리어 결과 화면", "Cleared: result screen"),
            ["over"] = ("게임 오버 결과 화면", "Game over: result screen"),
        };

        // Repeats of these say nothing new, so only the first one gets a subtitle.
        private static readonly HashSet<string> FirstOnly = new() { "aoe-cast", "aoe-dodged", "aoe-hit", "off-gcd" };

        public static string Line(string id, string detail, bool first, bool korean)
        {
            if (!first && FirstOnly.Contains(id) || !Lines.TryGetValue(id, out var line))
                return null;
            return string.Format(korean ? line.ko : line.en, detail);
        }

        public static string ToSrt(IReadOnlyList<GameEvent> events, bool korean)
        {
            var seen = new HashSet<string>();
            var cues = new List<(float start, List<string> lines)>();
            foreach (var e in events.OrderBy(e => e.Time))
            {
                var text = Line(e.Id, e.Detail, seen.Add(e.Id), korean);
                if (text == null)
                    continue;
                if (cues.Count > 0 && e.Time - cues[^1].start < GroupWindow)
                    cues[^1].lines.Add(text);
                else
                    cues.Add((e.Time, new List<string> { text }));
            }

            var srt = new StringBuilder();
            for (var i = 0; i < cues.Count; i++)
            {
                var start = cues[i].start;
                var end = start + MaxDuration;
                if (i + 1 < cues.Count)
                    end = Math.Min(end, cues[i + 1].start - Gap);
                srt.Append(i + 1).Append('\n')
                    .Append(Stamp(start)).Append(" --> ").Append(Stamp(end)).Append('\n')
                    .Append(string.Join("\n", cues[i].lines)).Append("\n\n");
            }
            return srt.ToString();
        }

        public static string ToTsv(IEnumerable<GameEvent> events)
        {
            var tsv = new StringBuilder("time\tevent\tdetail\n");
            foreach (var e in events)
                tsv.Append(e.Time.ToString("0.000", CultureInfo.InvariantCulture)).Append('\t')
                    .Append(e.Id).Append('\t').Append(e.Detail).Append('\n');
            return tsv.ToString();
        }

        public static List<GameEvent> FromTsv(string tsv) =>
            tsv.Split('\n').Skip(1).Where(row => row.Trim().Length > 0)
                .Select(row => row.TrimEnd('\r').Split('\t'))
                .Select(f => new GameEvent(float.Parse(f[0], CultureInfo.InvariantCulture), f[1], f.Length > 2 ? f[2] : ""))
                .ToList();

        public static string Stamp(float seconds)
        {
            var t = TimeSpan.FromMilliseconds(Math.Round(Math.Max(0f, seconds) * 1000.0));
            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00},{t.Milliseconds:000}";
        }
    }
}
