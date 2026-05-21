/* Tray popover, taskbar mock, and component style guide */

/* Tray popover that flies up from the system tray */
const TrayPopover = () => (
  <div style={{
    width: 320,
    background: "var(--surface-panel)",
    borderRadius: 10,
    border: "1px solid var(--stroke-default)",
    boxShadow: "var(--shadow-pop)",
    overflow: "hidden",
    fontFamily: "var(--font-ui)"
  }}>
    {/* Header */}
    <div style={{
      padding: "12px 14px",
      display: "flex", alignItems: "center", gap: 10,
      borderBottom: "1px solid var(--stroke-subtle)"
    }}>
      <IconTrayLogo size={22} />
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: 13, fontWeight: 600 }}>불쑥창닫개</div>
        <div style={{ fontSize: 11, color: "var(--text-tertiary)", display: "flex", alignItems: "center", gap: 5 }}>
          <span className="status-dot" style={{ width: 6, height: 6, boxShadow: "none" }} />
          감시 중 · 강도 적당
        </div>
      </div>
      <button className="btn btn-ghost btn-icon-only btn-sm" title="설정"><IconSettings size={14}/></button>
    </div>

    {/* Quick stats */}
    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", padding: "10px 14px",
                  gap: 8, borderBottom: "1px solid var(--stroke-subtle)" }}>
      <div>
        <div style={{ fontSize: 10, color: "var(--text-tertiary)", textTransform: "uppercase", letterSpacing: 0.04 }}>오늘 정리</div>
        <div className="num" style={{ fontSize: 18, fontWeight: 700 }}>37<span style={{ fontSize: 11, color: "var(--text-tertiary)", marginLeft: 2 }}>개</span></div>
      </div>
      <div>
        <div style={{ fontSize: 10, color: "var(--text-tertiary)", textTransform: "uppercase", letterSpacing: 0.04 }}>활성 규칙</div>
        <div className="num" style={{ fontSize: 18, fontWeight: 700 }}>4<span style={{ fontSize: 11, color: "var(--text-tertiary)", marginLeft: 2 }}>/5</span></div>
      </div>
    </div>

    {/* Last handled */}
    <div style={{ padding: "10px 14px", borderBottom: "1px solid var(--stroke-subtle)" }}>
      <div style={{ fontSize: 10, color: "var(--text-tertiary)", textTransform: "uppercase", letterSpacing: 0.04, marginBottom: 6 }}>
        방금 처리됨
      </div>
      {SAMPLE_HISTORY.slice(0,2).map((h, i) => (
        <div key={i} style={{ display: "flex", gap: 8, alignItems: "center", padding: "4px 0" }}>
          <ActionBadge action={h.action} />
          <div style={{ flex: 1, minWidth: 0, fontSize: 12,
                        overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
            {h.title}
          </div>
          <div className="num" style={{ fontSize: 10, color: "var(--text-tertiary)" }}>{h.t}</div>
        </div>
      ))}
    </div>

    {/* Actions */}
    <div style={{ padding: 10, display: "flex", flexDirection: "column", gap: 4 }}>
      <TrayItem icon={IconPause} label="일시정지" sub="다음 1시간" />
      <TrayItem icon={IconCamera} label="창 찍어서 규칙 만들기" />
      <TrayItem icon={IconHome} label="앱 열기" />
    </div>
  </div>
);

const TrayItem = ({ icon: IconC, label, sub }) => (
  <div style={{
    display: "flex", alignItems: "center", gap: 10,
    padding: "8px 8px",
    borderRadius: 6,
    cursor: "pointer",
    color: "var(--text-primary)",
    fontSize: 13
  }}
  onMouseEnter={(e) => e.currentTarget.style.background = "var(--surface-sunken)"}
  onMouseLeave={(e) => e.currentTarget.style.background = "transparent"}>
    <IconC size={16} style={{ color: "var(--text-secondary)" }} />
    <span style={{ flex: 1 }}>{label}</span>
    {sub && <span style={{ fontSize: 11, color: "var(--text-tertiary)" }}>{sub}</span>}
  </div>
);

/* Taskbar mock that shows where the tray sits */
const TaskbarMock = () => (
  <div style={{
    width: 720, height: 48,
    background: "rgba(243,243,243,0.96)",
    backdropFilter: "blur(20px)",
    borderRadius: 10,
    border: "1px solid var(--stroke-default)",
    boxShadow: "var(--shadow-2)",
    display: "flex", alignItems: "center",
    padding: "0 12px", gap: 6
  }}>
    {/* Centered apps */}
    <div style={{ flex: 1, display: "flex", justifyContent: "center", gap: 4 }}>
      {[0,1,2,3,4,5].map(i => (
        <div key={i} style={{
          width: 36, height: 36, borderRadius: 6,
          background: i === 2 ? "var(--surface-sunken)" : "transparent",
          display: "grid", placeItems: "center",
          color: "var(--text-secondary)"
        }}>
          <div style={{
            width: 16, height: 16, borderRadius: 3,
            background: ["#3b82f6","#10b981","#f59e0b","#8b5cf6","#ef4444","#6b7280"][i],
            opacity: 0.85
          }} />
        </div>
      ))}
    </div>
    {/* Tray area */}
    <div style={{ display: "flex", alignItems: "center", gap: 8,
                  padding: "4px 10px", borderRadius: 6,
                  background: "var(--surface-sunken)" }}>
      <div style={{ width: 14, height: 14, borderRadius: 2, background: "var(--text-tertiary)", opacity: 0.4 }} />
      <div style={{ width: 14, height: 14, borderRadius: 2, background: "var(--text-tertiary)", opacity: 0.4 }} />
      <div style={{ position: "relative" }}>
        <IconTrayLogo size={16} />
        <div style={{
          position: "absolute", top: -3, right: -4,
          width: 8, height: 8, borderRadius: "50%",
          background: "var(--accent-500)",
          border: "1.5px solid var(--surface-panel)"
        }} />
      </div>
    </div>
    {/* Clock */}
    <div style={{ padding: "0 10px 0 6px", fontSize: 11, color: "var(--text-secondary)", textAlign: "right", lineHeight: 1.2 }}>
      <div className="num">14:32</div>
      <div className="num" style={{ fontSize: 10, color: "var(--text-tertiary)" }}>2026-05-21</div>
    </div>
  </div>
);

/* Tray popover anchored above a tiny piece of the taskbar */
const TrayContext = () => (
  <div style={{
    width: 740, height: 480,
    background:
      "radial-gradient(1200px 600px at 50% 100%, #cdd6e0 0%, #aab4c1 60%, #8b96a6 100%)",
    borderRadius: 12,
    padding: 20,
    display: "flex", flexDirection: "column", justifyContent: "flex-end",
    alignItems: "flex-end",
    gap: 8,
    overflow: "hidden",
    position: "relative"
  }}>
    {/* fake wallpaper desktop hint */}
    <div style={{ position: "absolute", top: 16, left: 18, color: "rgba(255,255,255,0.85)", fontSize: 11, fontWeight: 500, letterSpacing: 0.05, textTransform: "uppercase" }}>
      Desktop · 트레이 팝오버
    </div>
    <TrayPopover />
    <TaskbarMock />
  </div>
);

/* ============================================================
   STYLE GUIDE — components reference card
   ============================================================ */
const StyleGuide = () => (
  <div style={{
    width: 900, padding: 24,
    background: "var(--surface-panel)",
    borderRadius: 12,
    border: "1px solid var(--stroke-default)",
    fontFamily: "var(--font-ui)",
    display: "flex", flexDirection: "column", gap: 20
  }}>
    <div>
      <div className="h-eyebrow">디자인 시스템</div>
      <h2 style={{ margin: "4px 0 0", fontSize: 22, fontWeight: 700, letterSpacing: "-0.01em" }}>
        컴포넌트 스타일 가이드
      </h2>
    </div>

    {/* Color */}
    <Section title="색상">
      <div style={{ display: "grid", gridTemplateColumns: "repeat(6, 1fr)", gap: 8 }}>
        <Swatch name="accent-500" varName="--accent-500" sub="포인트 컬러"/>
        <Swatch name="accent-700" varName="--accent-700" />
        <Swatch name="success"    varName="--success" />
        <Swatch name="warning"    varName="--warning" />
        <Swatch name="danger"     varName="--danger" sub="닫기·위험"/>
        <Swatch name="info"       varName="--info" />
      </div>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(6, 1fr)", gap: 8, marginTop: 8 }}>
        <Swatch name="canvas" varName="--surface-canvas" border/>
        <Swatch name="app"    varName="--surface-app" border/>
        <Swatch name="panel"  varName="--surface-panel" border/>
        <Swatch name="sunken" varName="--surface-sunken" border/>
        <Swatch name="text"   varName="--text-primary" />
        <Swatch name="muted"  varName="--text-tertiary" />
      </div>
    </Section>

    {/* Type */}
    <Section title="타이포">
      <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
        <div style={{ fontSize: 28, fontWeight: 700, letterSpacing: "-0.01em" }}>대시보드 28 / 700</div>
        <div style={{ fontSize: 20, fontWeight: 700, letterSpacing: "-0.01em" }}>화면 제목 20 / 700</div>
        <div style={{ fontSize: 14, fontWeight: 600 }}>섹션 제목 14 / 600</div>
        <div style={{ fontSize: 13 }}>본문 13 / 400 · 한국어 가독성을 위해 Pretendard 우선</div>
        <div style={{ fontSize: 12, color: "var(--text-secondary)" }}>보조 12 / 400 · 회색 텍스트</div>
        <div style={{ fontSize: 11, color: "var(--text-tertiary)", textTransform: "uppercase", letterSpacing: 0.04, fontWeight: 600 }}>EYEBROW 11 / 600 UPPER</div>
        <div style={{ fontFamily: "var(--font-mono)", fontSize: 12 }}>mono · ShopAssist.exe · 480×320</div>
      </div>
    </Section>

    {/* Buttons */}
    <Section title="버튼">
      <div style={{ display: "flex", flexWrap: "wrap", gap: 8, alignItems: "center" }}>
        <button className="btn btn-primary"><IconPlus size={14}/> 새 규칙</button>
        <button className="btn">취소</button>
        <button className="btn btn-ghost">설정 열기</button>
        <button className="btn btn-danger-ghost"><IconTrash size={14}/> 삭제</button>
        <button className="btn btn-sm">작게</button>
        <button className="btn btn-lg btn-primary">큰 버튼</button>
        <div className="segmented">
          <button className="on">전체</button>
          <button>활성</button>
          <button>비활성</button>
        </div>
      </div>
    </Section>

    {/* Toggles + slider */}
    <Section title="토글 · 슬라이더">
      <div style={{ display: "flex", gap: 24, alignItems: "center", flexWrap: "wrap" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div className="toggle" /><span style={{ fontSize: 12 }}>off</span>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div className="toggle on" /><span style={{ fontSize: 12 }}>on</span>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div className="toggle sm on" /><span style={{ fontSize: 12 }}>small</span>
        </div>
        <div style={{ flex: 1, minWidth: 280, maxWidth: 380 }}>
          <div className="intensity-slider">
            <div className="slider-track">
              <div className="slider-rail" />
              <div className="slider-fill" style={{ width: "66%" }} />
              <div className="slider-ticks"><span /><span /><span /><span /></div>
              <div className="slider-thumb" style={{ left: "66%" }} />
            </div>
            <div className="slider-labels">
              <span>구경만</span><span>조심</span><span className="active">적당</span><span>적극</span>
            </div>
          </div>
        </div>
      </div>
    </Section>

    {/* Badges */}
    <Section title="배지">
      <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
        <ActionBadge action="minimize" />
        <ActionBadge action="hide" />
        <ActionBadge action="close" />
        <ActionBadge action="log" />
        <span className="badge status-active"><span className="badge-dot"/>활성</span>
        <span className="badge status-paused"><span className="badge-dot"/>일시정지</span>
        <span className="badge subtle"><IconShield size={10}/> 보호됨</span>
        <span className="badge subtle num">반복 12</span>
      </div>
    </Section>

    {/* Chips & rule preview */}
    <Section title="매칭 기준 칩">
      <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
        <span className="chip"><span className="chip-key">제목</span><span className="chip-val">특가 *</span></span>
        <span className="chip"><span className="chip-key">앱</span><span className="chip-val">ShopAssist.exe</span></span>
        <span className="chip"><span className="chip-key">클래스</span><span className="chip-val">AdPopupWindow</span></span>
        <span className="chip"><span className="chip-key">크기</span><span className="chip-val">&lt;500×400</span></span>
        <span className="chip"><span className="chip-key">위치</span><span className="chip-val">우하단</span></span>
      </div>
    </Section>

    {/* Microcopy */}
    <Section title="한국어 마이크로카피 원칙">
      <ul style={{ margin: 0, paddingLeft: 18, fontSize: 12, color: "var(--text-secondary)", lineHeight: 1.8 }}>
        <li>동작은 <b>동사+명사</b>로: "규칙 만들기", "창 찍기", "다시 시작".</li>
        <li>상태는 <b>형용사+ㅁ</b>으로: "감시 중", "일시정지됨", "보호됨".</li>
        <li>강도는 일상어로: <span className="chip"><span className="chip-val">구경만</span></span> · <span className="chip"><span className="chip-val">조심</span></span> · <span className="chip"><span className="chip-val">적당</span></span> · <span className="chip"><span className="chip-val">적극</span></span>.</li>
        <li>위험 액션은 <b>경고 + 권장 대안</b>을 함께 안내. ("기록만 모드로 며칠 지켜본 뒤…")</li>
        <li>한자어 줄이고 친근하게: "정리", "내리기", "찍어서 규칙 만들기".</li>
      </ul>
    </Section>

    {/* WPF mapping suggestion */}
    <Section title="기존 WPF 구조와의 매핑">
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, fontSize: 12, color: "var(--text-secondary)", lineHeight: 1.7 }}>
        <div>
          <div className="h-eyebrow" style={{ marginBottom: 6 }}>화면 → XAML</div>
          <ul style={{ margin: 0, paddingLeft: 18 }}>
            <li><code>MainWindow.xaml</code> = TitleBar + Sidebar + StatusStrip + ContentFrame</li>
            <li><code>DashboardPage.xaml</code> · <code>RulesPage.xaml</code> · <code>DetectedPage.xaml</code> · <code>HistoryPage.xaml</code> · <code>SettingsPage.xaml</code></li>
            <li><code>RuleEditorPage.xaml</code> = Frame 안에서 열리는 별도 페이지 (또는 ContentDialog)</li>
            <li><code>TrayPopoverWindow.xaml</code> = NotifyIcon 클릭 시 우하단 정렬 ToolWindow</li>
          </ul>
        </div>
        <div>
          <div className="h-eyebrow" style={{ marginBottom: 6 }}>컴포넌트 → UserControl</div>
          <ul style={{ margin: 0, paddingLeft: 18 }}>
            <li><code>StatusStripControl</code> · <code>IntensitySlider</code> · <code>ActionBadge</code></li>
            <li><code>DetectedWindowRow</code> · <code>RuleRow</code> · <code>HistoryRow</code></li>
            <li><code>WindowThumbnail</code> = DWM 썸네일 또는 placeholder</li>
            <li>토큰은 <code>App.xaml</code> ResourceDictionary로 색·간격·라운드 정의</li>
          </ul>
        </div>
      </div>
    </Section>
  </div>
);

const Section = ({ title, children }) => (
  <section>
    <div className="h-eyebrow" style={{ marginBottom: 10 }}>{title}</div>
    {children}
  </section>
);

const Swatch = ({ name, varName, sub, border }) => (
  <div style={{
    border: "1px solid var(--stroke-subtle)",
    borderRadius: 8,
    overflow: "hidden",
    background: "var(--surface-panel)"
  }}>
    <div style={{
      height: 48,
      background: `var(${varName})`,
      borderBottom: border ? "1px solid var(--stroke-subtle)" : "none"
    }} />
    <div style={{ padding: "6px 8px" }}>
      <div style={{ fontSize: 11, fontWeight: 600 }}>{name}</div>
      {sub && <div style={{ fontSize: 10, color: "var(--text-tertiary)" }}>{sub}</div>}
    </div>
  </div>
);

Object.assign(window, { TrayPopover, TaskbarMock, TrayContext, StyleGuide });
