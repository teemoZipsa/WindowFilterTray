/* All screen content components */

/* ============================================================
   Shared data
   ============================================================ */
const SAMPLE_WINDOWS = [
  { title: "특가 이벤트! 지금 확인하세요", app: "ShopAssist.exe", cls: "AdPopupWindow", time: "방금 전", icon: "tag", repeat: 12 },
  { title: "업데이트가 준비되었습니다", app: "Updater.exe",   cls: "Win32Updater",    time: "1분 전",  icon: "down", repeat: 3 },
  { title: "Notion – 회의록 (2026 Q2)", app: "Notion.exe",   cls: "Chrome_WidgetWin_1", time: "3분 전",  icon: "doc",  repeat: 0 },
  { title: "쿠폰이 도착했어요 🎁",        app: "DeliveryApp.exe", cls: "PopBalloon",  time: "8분 전",  icon: "gift", repeat: 7 },
  { title: "AhnLab Safe Transaction",   app: "ASTSvc.exe",   cls: "ASTMain",        time: "12분 전", icon: "lock", repeat: 1, protected: true },
];

const SAMPLE_RULES = [
  { id: 1, name: "쇼핑몰 광고 팝업 내리기", action: "minimize", enabled: true,  match: [["app","ShopAssist.exe"],["title","특가 *"]], last: "방금 전", count: 142, intensity: 3 },
  { id: 2, name: "배달앱 쿠폰창 숨기기",    action: "hide",     enabled: true,  match: [["app","DeliveryApp.exe"],["class","PopBalloon"]], last: "8분 전", count: 87, intensity: 2 },
  { id: 3, name: "업데이트 안내 작게 내리기", action: "minimize", enabled: true,  match: [["title","* 업데이트 *"]], last: "1분 전", count: 34, intensity: 1 },
  { id: 4, name: "오래된 토스트 자동 닫기",  action: "close",    enabled: false, match: [["class","ToastWindow"],["size","<400×120"]], last: "어제", count: 19, intensity: 3 },
  { id: 5, name: "회의 중 모든 팝업 기록만", action: "log",      enabled: true,  match: [["schedule","09:00–18:00 평일"]], last: "오늘 14:32", count: 8, intensity: 0 },
];

const SAMPLE_HISTORY = [
  { t: "14:32:08", title: "특가 이벤트! 지금 확인하세요", app: "ShopAssist.exe", rule: "쇼핑몰 광고 팝업 내리기", action: "minimize", score: 0.94, reason: "광고 키워드 + 알려진 프로세스" },
  { t: "14:31:55", title: "쿠폰이 도착했어요 🎁", app: "DeliveryApp.exe", rule: "배달앱 쿠폰창 숨기기", action: "hide", score: 0.88, reason: "클래스 일치" },
  { t: "14:30:12", title: "업데이트가 준비되었습니다", app: "Updater.exe", rule: "업데이트 안내 작게 내리기", action: "minimize", score: 0.79, reason: "제목 패턴 일치" },
  { t: "14:25:03", title: "오류 보고를 보내시겠습니까?", app: "Outlook.exe", rule: "—", action: "log", score: 0.41, reason: "기준 미달 / 안전 정책" },
  { t: "14:21:47", title: "특가 이벤트! 지금 확인하세요", app: "ShopAssist.exe", rule: "쇼핑몰 광고 팝업 내리기", action: "minimize", score: 0.96, reason: "광고 키워드 + 알려진 프로세스" },
  { t: "14:19:30", title: "AhnLab Safe Transaction", app: "ASTSvc.exe", rule: "(보호됨)", action: "skip", score: null, reason: "시스템 보호 정책" },
  { t: "14:12:08", title: "쇼핑 알림", app: "ShopAssist.exe", rule: "쇼핑몰 광고 팝업 내리기", action: "minimize", score: 0.71, reason: "프로세스 일치" },
];

const ActionBadge = ({ action }) => {
  const map = {
    minimize: { cls: "action-minimize", label: "작게 내리기" },
    hide:     { cls: "action-hide",     label: "숨기기" },
    close:    { cls: "action-close",    label: "닫기" },
    log:      { cls: "action-log",      label: "기록만" },
    skip:     { cls: "subtle",          label: "건너뜀" },
  };
  const it = map[action] || map.log;
  return <span className={"badge " + it.cls}><span className="badge-dot" />{it.label}</span>;
};

const MatchChips = ({ match }) => (
  <div style={{ display: "flex", flexWrap: "wrap", gap: 4 }}>
    {match.map(([k, v], i) => {
      const keyLabel = ({app:"앱", title:"제목", class:"클래스", size:"크기", pos:"위치", schedule:"시간"})[k] || k;
      return (
        <span key={i} className="chip">
          <span className="chip-key">{keyLabel}</span>
          <span className="chip-val">{v}</span>
        </span>
      );
    })}
  </div>
);

/* ============================================================
   DASHBOARD
   ============================================================ */
const Dashboard = () => (
  <div className="page">
    <div className="page-header">
      <div>
        <h1 className="page-title">대시보드</h1>
        <div className="page-subtitle">최근 화면에 떠오른 창과 지금 작동 중인 규칙을 한눈에 봅니다.</div>
      </div>
      <div style={{ display: "flex", gap: 8 }}>
        <button className="btn">
          <IconLog size={14}/> 기록 보기
        </button>
        <button className="btn btn-primary">
          <IconCamera size={14}/> 창 찍어서 규칙 만들기
        </button>
      </div>
    </div>

    {/* Hero strip — two summary tiles + tip */}
    <div style={{ display: "grid", gridTemplateColumns: "1.2fr 1.2fr 1fr", gap: 12, marginBottom: 16 }}>
      <div className="card" style={{ padding: 16 }}>
        <div className="h-eyebrow">오늘 추이</div>
        <div style={{ display: "flex", alignItems: "baseline", gap: 8, marginTop: 6 }}>
          <div style={{ fontSize: 28, fontWeight: 700 }} className="num">37</div>
          <div style={{ fontSize: 12, color: "var(--text-tertiary)" }}>개 창을 정리했어요</div>
        </div>
        {/* tiny bar chart */}
        <div style={{ display: "flex", alignItems: "flex-end", gap: 3, height: 36, marginTop: 12 }}>
          {[6,3,5,8,4,2,3,5,9,12,7,4,6,8,5,3,4].map((v,i) => (
            <div key={i} style={{
              width: 8, height: v*3,
              background: i === 9 ? "var(--accent-500)" : "var(--accent-200)",
              borderRadius: 2
            }} />
          ))}
        </div>
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: 10, color: "var(--text-tertiary)", marginTop: 6 }}>
          <span>오전 6시</span><span>정오</span><span>오후 6시</span><span>지금</span>
        </div>
      </div>

      <div className="card" style={{ padding: 16 }}>
        <div className="h-eyebrow">방금 처리된 창</div>
        <div style={{ marginTop: 8, display: "flex", flexDirection: "column", gap: 8 }}>
          {SAMPLE_HISTORY.slice(0,3).map((h,i) => (
            <div key={i} style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <span style={{ fontSize: 11, color: "var(--text-tertiary)", width: 56 }} className="num">{h.t}</span>
              <span style={{
                flex: 1, fontSize: 12, color: "var(--text-primary)",
                overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap"
              }}>{h.title}</span>
              <ActionBadge action={h.action} />
            </div>
          ))}
        </div>
      </div>

      <div className="card" style={{ padding: 16, background: "linear-gradient(180deg, var(--accent-50), var(--surface-panel))" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 6, color: "var(--accent-700)" }}>
          <IconSpark size={14} />
          <div className="h-eyebrow" style={{ color: "var(--accent-700)" }}>오늘의 팁</div>
        </div>
        <div style={{ fontSize: 13, color: "var(--text-primary)", marginTop: 6, lineHeight: 1.5 }}>
          <b>ShopAssist.exe</b> 가 오늘 12번 떠올랐어요. 한 번에 규칙을 만들어 정리할까요?
        </div>
        <button className="btn btn-sm" style={{ marginTop: 10 }}>
          한 번에 규칙 만들기 <IconArrow size={12} />
        </button>
      </div>
    </div>

    {/* Recent detected list + frequent table */}
    <div style={{ display: "grid", gridTemplateColumns: "1.4fr 1fr", gap: 12 }}>
      <div className="card">
        <div className="card-header">
          <div className="card-title">최근 감지된 창</div>
          <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
            <span className="card-meta">실시간</span>
            <span className="status-dot" style={{ width: 6, height: 6 }} />
          </div>
        </div>
        <div>
          {SAMPLE_WINDOWS.slice(0,4).map((w,i) => (
            <DetectedRow key={i} w={w} compact />
          ))}
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <div className="card-title">자주 뜨는 창</div>
          <div className="card-meta">최근 7일</div>
        </div>
        <div style={{ padding: "4px 0" }}>
          {[
            { name: "특가 이벤트! …", app: "ShopAssist.exe", n: 84 },
            { name: "쿠폰이 도착했어요", app: "DeliveryApp.exe", n: 52 },
            { name: "업데이트가 준비…", app: "Updater.exe", n: 28 },
            { name: "리뷰 요청", app: "Editor.exe", n: 14 },
          ].map((r,i) => (
            <div key={i} style={{
              display: "flex", alignItems: "center", gap: 10,
              padding: "8px 16px",
              borderTop: i ? "1px solid var(--stroke-subtle)" : "none"
            }}>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontSize: 12, fontWeight: 500, color: "var(--text-primary)",
                  overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{r.name}</div>
                <div style={{ fontSize: 11, color: "var(--text-tertiary)", fontFamily: "var(--font-mono)" }}>{r.app}</div>
              </div>
              <span className="badge subtle num">{r.n}회</span>
              <button className="btn btn-sm">규칙</button>
            </div>
          ))}
        </div>
      </div>
    </div>
  </div>
);

/* Reusable row for detected windows */
const DetectedRow = ({ w, compact = false }) => (
  <div style={{
    display: "flex", alignItems: "center", gap: 12,
    padding: compact ? "10px 16px" : "12px 16px",
    borderTop: "1px solid var(--stroke-subtle)"
  }}>
    <div className="win-thumb" />
    <div style={{ flex: 1, minWidth: 0 }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <div style={{
          fontSize: 13, fontWeight: 500, color: "var(--text-primary)",
          overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", maxWidth: 280
        }}>{w.title}</div>
        {w.protected && <span className="badge subtle"><IconShield size={10}/> 보호됨</span>}
        {w.repeat > 0 && !w.protected && <span className="badge subtle num">반복 {w.repeat}</span>}
      </div>
      <div style={{ fontSize: 11, color: "var(--text-tertiary)", marginTop: 2, fontFamily: "var(--font-mono)" }}>
        {w.app} · <span style={{ color: "var(--text-tertiary)" }}>{w.cls}</span>
      </div>
    </div>
    <span style={{ fontSize: 11, color: "var(--text-tertiary)" }}>{w.time}</span>
    {!w.protected ? (
      <div style={{ display: "flex", gap: 4 }}>
        <button className="btn btn-sm" title="작게 내리기"><IconMinimize size={12}/></button>
        <button className="btn btn-sm" title="숨기기"><IconEyeOff size={12}/></button>
        <button className="btn btn-primary btn-sm">규칙 만들기</button>
      </div>
    ) : (
      <span className="hint"><IconShield size={12}/> 시스템 정책</span>
    )}
  </div>
);

/* ============================================================
   RULES LIST
   ============================================================ */
const RulesScreen = () => (
  <div className="page">
    <div className="page-header">
      <div>
        <h1 className="page-title">정리 규칙</h1>
        <div className="page-subtitle">규칙 5개 · 활성 4개 · 오늘 처리 290회</div>
      </div>
      <div style={{ display: "flex", gap: 8 }}>
        <div className="input-with-icon">
          <IconSearch className="lead-icon" size={14}/>
          <input className="input search" placeholder="규칙, 앱, 제목으로 검색"/>
        </div>
        <button className="btn"><IconFilter size={14}/> 필터</button>
        <button className="btn btn-primary"><IconPlus size={14}/> 새 규칙</button>
      </div>
    </div>

    <div className="card">
      <div style={{
        display: "grid",
        gridTemplateColumns: "44px 1.8fr 1.8fr 130px 140px 140px 80px",
        padding: "10px 16px",
        fontSize: 11, fontWeight: 600,
        color: "var(--text-tertiary)", textTransform: "uppercase", letterSpacing: 0.02,
        borderBottom: "1px solid var(--stroke-subtle)"
      }}>
        <div></div>
        <div>규칙</div>
        <div>매칭 기준</div>
        <div>처리 방식</div>
        <div>마지막 처리</div>
        <div>강도</div>
        <div></div>
      </div>
      {SAMPLE_RULES.map((r,i) => (
        <div key={r.id} style={{
          display: "grid",
          gridTemplateColumns: "44px 1.8fr 1.8fr 130px 140px 140px 80px",
          alignItems: "center",
          padding: "12px 16px",
          borderTop: i ? "1px solid var(--stroke-subtle)" : "none",
          opacity: r.enabled ? 1 : 0.6
        }}>
          <div className={"toggle sm" + (r.enabled ? " on" : "")} />
          <div>
            <div style={{ fontSize: 13, fontWeight: 600 }}>{r.name}</div>
            <div style={{ fontSize: 11, color: "var(--text-tertiary)", marginTop: 2 }} className="num">
              실행 {r.count}회
            </div>
          </div>
          <MatchChips match={r.match} />
          <ActionBadge action={r.action} />
          <div style={{ fontSize: 12, color: "var(--text-secondary)" }}>{r.last}</div>
          <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
            <IntensityDots level={r.intensity} />
            <span style={{ fontSize: 11, color: "var(--text-tertiary)" }}>{INTENSITY_LABELS[r.intensity]}</span>
          </div>
          <div style={{ display: "flex", justifyContent: "flex-end", gap: 2 }}>
            <button className="btn btn-ghost btn-icon-only btn-sm" title="편집"><IconEdit size={14}/></button>
            <button className="btn btn-ghost btn-icon-only btn-sm" title="더보기"><IconMore size={14}/></button>
          </div>
        </div>
      ))}
    </div>
  </div>
);

/* ============================================================
   DETECTED WINDOWS
   ============================================================ */
const DetectedScreen = () => (
  <div className="page">
    <div className="page-header">
      <div>
        <h1 className="page-title">최근 감지 창</h1>
        <div className="page-subtitle">방금 화면에 떠오른 창부터 차례대로 보입니다. 클릭해서 규칙을 만드세요.</div>
      </div>
      <div style={{ display: "flex", gap: 8 }}>
        <div className="segmented">
          <button className="on">전체</button>
          <button>처리됨</button>
          <button>건너뜀</button>
          <button>보호됨</button>
        </div>
        <button className="btn"><IconCamera size={14}/> 창 찍기</button>
      </div>
    </div>

    <div className="card">
      {SAMPLE_WINDOWS.map((w,i) => (
        <DetectedRow key={i} w={w} />
      ))}
    </div>

    <div style={{ marginTop: 12, fontSize: 11, color: "var(--text-tertiary)", display: "flex", alignItems: "center", gap: 6 }}>
      <IconInfo size={12} />
      목록은 최근 200개까지 보관됩니다. 더 오래된 기록은 <a style={{ color: "var(--accent-600)" }}>처리 기록</a>에서 확인하세요.
    </div>
  </div>
);

/* ============================================================
   HISTORY
   ============================================================ */
const HistoryScreen = () => (
  <div className="page">
    <div className="page-header">
      <div>
        <h1 className="page-title">처리 기록</h1>
        <div className="page-subtitle">오늘 290건 처리 · 평균 점수 0.81</div>
      </div>
      <div style={{ display: "flex", gap: 8 }}>
        <div className="input-with-icon">
          <IconSearch className="lead-icon" size={14}/>
          <input className="input search" placeholder="제목 또는 앱 검색"/>
        </div>
        <button className="btn"><IconClock size={14}/> 오늘</button>
        <button className="btn"><IconFilter size={14}/> 액션</button>
      </div>
    </div>

    <div className="card">
      <div style={{
        display: "grid",
        gridTemplateColumns: "90px 1.6fr 140px 1.4fr 120px 70px",
        padding: "10px 16px",
        fontSize: 11, fontWeight: 600,
        color: "var(--text-tertiary)", textTransform: "uppercase",
        borderBottom: "1px solid var(--stroke-subtle)"
      }}>
        <div>시각</div>
        <div>창 제목</div>
        <div>앱</div>
        <div>적용 규칙 / 이유</div>
        <div>액션</div>
        <div style={{ textAlign: "right" }}>점수</div>
      </div>
      {SAMPLE_HISTORY.map((h,i) => (
        <div key={i} style={{
          display: "grid",
          gridTemplateColumns: "90px 1.6fr 140px 1.4fr 120px 70px",
          alignItems: "center",
          padding: "10px 16px",
          borderTop: i ? "1px solid var(--stroke-subtle)" : "none",
          fontSize: 12
        }}>
          <div className="num" style={{ color: "var(--text-tertiary)", fontFamily: "var(--font-mono)" }}>{h.t}</div>
          <div style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", color: "var(--text-primary)", fontWeight: 500 }}>
            {h.title}
          </div>
          <div style={{ color: "var(--text-secondary)", fontFamily: "var(--font-mono)", fontSize: 11 }}>{h.app}</div>
          <div>
            <div style={{ color: "var(--text-primary)" }}>{h.rule}</div>
            <div style={{ fontSize: 11, color: "var(--text-tertiary)", marginTop: 1 }}>{h.reason}</div>
          </div>
          <div><ActionBadge action={h.action} /></div>
          <div className="num" style={{
            textAlign: "right",
            color: h.score == null ? "var(--text-tertiary)" : h.score > 0.7 ? "var(--accent-700)" : "var(--text-secondary)",
            fontWeight: 600
          }}>
            {h.score == null ? "—" : h.score.toFixed(2)}
          </div>
        </div>
      ))}
    </div>
  </div>
);

/* ============================================================
   SETTINGS
   ============================================================ */
const SettingsScreen = () => (
  <div className="page">
    <div className="page-header">
      <div>
        <h1 className="page-title">설정</h1>
        <div className="page-subtitle">감시 정책과 안전 정책을 조정합니다.</div>
      </div>
    </div>

    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
      {/* Intensity card — wide */}
      <div className="card" style={{ gridColumn: "1 / span 2", padding: 18 }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 4 }}>
          <div className="card-title">기본 정리 강도</div>
          <span className="pill-intensity">
            <IntensityDots level={2} />
            적당
          </span>
        </div>
        <div style={{ fontSize: 12, color: "var(--text-tertiary)", marginBottom: 14 }}>
          새 규칙을 만들 때 기본값으로 쓰입니다. 강도가 높을수록 적극적으로 창을 정리합니다.
        </div>
        <div className="intensity-slider">
          <div className="slider-track">
            <div className="slider-rail" />
            <div className="slider-fill" style={{ width: "66%" }} />
            <div className="slider-ticks">
              <span /><span /><span /><span />
            </div>
            <div className="slider-thumb" style={{ left: "66%" }} />
          </div>
          <div className="slider-labels">
            <span>구경만</span>
            <span>조심</span>
            <span className="active">적당</span>
            <span>적극</span>
          </div>
        </div>
        <div style={{ fontSize: 12, color: "var(--text-secondary)", marginTop: 12, lineHeight: 1.6,
                      background: "var(--surface-sunken)", borderRadius: 8, padding: "10px 12px" }}>
          <b>적당</b> · 알려진 광고/팝업 패턴에 일치하면 자동으로 작게 내립니다.
          닫기 액션은 사용자가 만든 규칙에서만 사용됩니다.
        </div>
      </div>

      <div className="card" style={{ padding: 18 }}>
        <div className="card-title" style={{ marginBottom: 6 }}>일반</div>
        <div style={{ borderTop: "1px solid var(--stroke-subtle)", marginTop: 6 }}>
          <div className="toggle-row">
            <div className="toggle-text">
              <div className="toggle-title">Windows 시작 시 자동 실행</div>
              <div className="toggle-desc">트레이에 숨겨진 상태로 함께 켜집니다.</div>
            </div>
            <div className="toggle on" />
          </div>
          <div className="toggle-row" style={{ borderTop: "1px solid var(--stroke-subtle)" }}>
            <div className="toggle-text">
              <div className="toggle-title">처리 시 짧은 알림 표시</div>
              <div className="toggle-desc">트레이에서 1초간 작게 알려줍니다.</div>
            </div>
            <div className="toggle on" />
          </div>
          <div className="toggle-row" style={{ borderTop: "1px solid var(--stroke-subtle)" }}>
            <div className="toggle-text">
              <div className="toggle-title">전체 화면 앱에서는 멈추기</div>
              <div className="toggle-desc">게임, 발표 모드에서는 자동 일시정지.</div>
            </div>
            <div className="toggle" />
          </div>
        </div>
      </div>

      <div className="card" style={{ padding: 18 }}>
        <div className="card-title" style={{ marginBottom: 6, display: "flex", alignItems: "center", gap: 6 }}>
          <IconShield size={14} style={{ color: "var(--accent-600)"}} /> 안전 정책
        </div>
        <div style={{ fontSize: 12, color: "var(--text-tertiary)", marginBottom: 6 }}>
          시스템·보안 창은 절대 자동으로 닫지 않습니다.
        </div>
        <div style={{ borderTop: "1px solid var(--stroke-subtle)" }}>
          <div className="toggle-row">
            <div className="toggle-text">
              <div className="toggle-title">시스템 창 보호</div>
              <div className="toggle-desc">UAC, Windows 보안, 백신 알림은 건드리지 않음.</div>
            </div>
            <div className="toggle on" />
          </div>
          <div className="toggle-row" style={{ borderTop: "1px solid var(--stroke-subtle)" }}>
            <div className="toggle-text">
              <div className="toggle-title">관리자 권한 창 보호</div>
              <div className="toggle-desc">권한 상승된 창에는 닫기 액션을 막습니다.</div>
            </div>
            <div className="toggle on" />
          </div>
          <div className="toggle-row" style={{ borderTop: "1px solid var(--stroke-subtle)" }}>
            <div className="toggle-text">
              <div className="toggle-title">처음 본 창은 5초 지켜보기</div>
              <div className="toggle-desc">사용자 의도가 있어 보이면 그대로 둡니다.</div>
            </div>
            <div className="toggle on" />
          </div>
        </div>
      </div>
    </div>
  </div>
);

Object.assign(window, {
  SAMPLE_WINDOWS, SAMPLE_RULES, SAMPLE_HISTORY,
  ActionBadge, MatchChips, DetectedRow,
  Dashboard, RulesScreen, DetectedScreen, HistoryScreen, SettingsScreen,
});
