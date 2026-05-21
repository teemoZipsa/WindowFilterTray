/* Shared shell pieces: TitleBar, Sidebar, StatusStrip, AppWindow wrapper */

const TitleBar = ({ title = "불쑥창닫개" }) => (
  <div className="titlebar">
    <div className="tb-app">
      <div className="tb-logo" />
      <span>{title}</span>
    </div>
    <div className="spacer" />
    <div className="tb-controls">
      <button className="tb-btn" title="최소화"><IconMinimize size={14} /></button>
      <button className="tb-btn" title="최대화"><IconMaximize size={12} /></button>
      <button className="tb-btn close" title="닫기"><IconClose size={14} /></button>
    </div>
  </div>
);

const Sidebar = ({ active = "dashboard", counts = {} }) => {
  const items = [
    { id: "dashboard", icon: IconHome,    label: "대시보드" },
    { id: "rules",     icon: IconRules,   label: "정리 규칙",   count: counts.rules },
    { id: "detected",  icon: IconWindow,  label: "최근 감지 창", count: counts.detected },
    { id: "history",   icon: IconHistory, label: "처리 기록" },
    { id: "settings",  icon: IconSettings,label: "설정" },
  ];
  return (
    <nav className="sidebar">
      <div className="sb-section">탐색</div>
      {items.map(it => {
        const IconC = it.icon;
        const isActive = active === it.id;
        return (
          <div key={it.id} className={"sb-item" + (isActive ? " active" : "")}>
            <IconC className="sb-icon" />
            <span className="sb-label">{it.label}</span>
            {it.count != null && <span className="sb-count num">{it.count}</span>}
          </div>
        );
      })}
      <div style={{ flex: 1 }} />
      <div className="sb-section">상태</div>
      <div className="sb-item" style={{ cursor: "default" }}>
        <IconShield className="sb-icon" />
        <span className="sb-label" style={{ fontSize: 12, color: "var(--text-tertiary)" }}>
          시스템 창 보호
        </span>
        <div className="toggle on sm" />
      </div>
    </nav>
  );
};

const IntensityDots = ({ level = 2 }) => (
  <span className="intensity-dots">
    {[0,1,2,3].map(i => <span key={i} className={i <= level ? "on" : ""} />)}
  </span>
);

const INTENSITY_LABELS = ["구경만", "조심", "적당", "적극"];

const StatusStrip = ({
  state = "active",        // active | paused
  intensity = 2,
  cleanedToday = 0,
  onTogglePause = () => {},
}) => {
  const isPaused = state === "paused";
  return (
    <div className="status-strip">
      <div className="status-block">
        <span className={"status-dot" + (isPaused ? " paused" : "")} />
        <div>
          <div className="label">현재 상태</div>
          <div className="value">{isPaused ? "일시정지됨" : "감시 중"}</div>
        </div>
      </div>
      <div className="divider" />
      <div className="status-block">
        <div>
          <div className="label">정리 강도</div>
          <div style={{ display: "flex", alignItems: "center", gap: 8, marginTop: 2 }}>
            <span className="pill-intensity">
              <IntensityDots level={intensity} />
              {INTENSITY_LABELS[intensity]}
            </span>
          </div>
        </div>
      </div>
      <div className="divider" />
      <div className="status-block">
        <div>
          <div className="label">오늘 정리한 창</div>
          <div className="value num">{cleanedToday}<span style={{ fontSize: 12, color: "var(--text-tertiary)", fontWeight: 500, marginLeft: 4 }}>개</span></div>
        </div>
      </div>
      <div style={{ flex: 1 }} />
      <button className={"btn " + (isPaused ? "btn-primary" : "")}>
        {isPaused ? <><IconPlay size={14}/> 다시 시작</> : <><IconPause size={14}/> 빠른 일시정지</>}
      </button>
    </div>
  );
};

/* AppWindow with title bar + sidebar + main column */
const AppWindow = ({ active, counts, status, children, title }) => (
  <div className="app-window">
    <TitleBar title={title} />
    <div className="app-body">
      <Sidebar active={active} counts={counts} />
      <div className="main">
        <StatusStrip {...status} />
        {children}
      </div>
    </div>
  </div>
);

Object.assign(window, { TitleBar, Sidebar, StatusStrip, AppWindow, IntensityDots, INTENSITY_LABELS });
