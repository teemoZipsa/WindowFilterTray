/* Rule editor — modal/screen for creating or editing a rule */

const RuleEditor = () => (
  <div className="page" style={{ background: "var(--surface-canvas)" }}>
    {/* breadcrumb header */}
    <div style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 12, color: "var(--text-tertiary)", marginBottom: 8 }}>
      <span>정리 규칙</span>
      <IconChevron size={12} />
      <span style={{ color: "var(--text-primary)" }}>새 규칙 만들기</span>
    </div>

    <div className="page-header" style={{ marginBottom: 12 }}>
      <div>
        <h1 className="page-title">새 규칙 만들기</h1>
        <div className="page-subtitle">감지된 창에서 시작하면 매칭 기준이 자동으로 채워집니다.</div>
      </div>
      <div style={{ display: "flex", gap: 8 }}>
        <button className="btn">취소</button>
        <button className="btn btn-primary">규칙 저장</button>
      </div>
    </div>

    <div style={{ display: "grid", gridTemplateColumns: "1fr 280px", gap: 12 }}>
      {/* Left — form */}
      <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>

        {/* Step 1 — basics */}
        <div className="card" style={{ padding: 18 }}>
          <StepHeader n="1" title="기본" subtitle="규칙 이름과 어떤 창을 정리할지" />

          <FieldRow label="규칙 이름">
            <input className="input" defaultValue="쇼핑몰 광고 팝업 내리기" style={{ width: "100%" }} />
          </FieldRow>

          <FieldRow label="기준 창" hint="감지된 창에서 자동 채움">
            <div style={{
              display: "flex", gap: 12, alignItems: "center",
              background: "var(--surface-sunken)", borderRadius: 8, padding: 10,
              border: "1px solid var(--stroke-subtle)"
            }}>
              <div className="win-thumb" />
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontSize: 13, fontWeight: 500 }}>특가 이벤트! 지금 확인하세요</div>
                <div style={{ fontSize: 11, color: "var(--text-tertiary)", marginTop: 2, fontFamily: "var(--font-mono)" }}>
                  ShopAssist.exe · AdPopupWindow · 480×320
                </div>
              </div>
              <button className="btn btn-sm"><IconCamera size={12}/> 다시 찍기</button>
            </div>
          </FieldRow>
        </div>

        {/* Step 2 — match */}
        <div className="card" style={{ padding: 18 }}>
          <StepHeader n="2" title="이런 창에 적용" subtitle="모든 조건이 일치할 때만 실행됩니다" />

          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            <CriterionRow on label="제목" hint="와일드카드 * 사용 가능">
              <input className="input mono" defaultValue="특가 *" style={{ width: "100%" }} />
            </CriterionRow>
            <CriterionRow on label="앱 (프로세스)">
              <input className="input mono" defaultValue="ShopAssist.exe" style={{ width: "100%" }} />
            </CriterionRow>
            <CriterionRow label="창 클래스 (고급)">
              <input className="input mono" placeholder="AdPopupWindow" style={{ width: "100%" }} disabled />
            </CriterionRow>

            {/* Advanced collapse */}
            <details style={{ marginTop: 6 }}>
              <summary style={{
                cursor: "pointer", listStyle: "none",
                display: "flex", alignItems: "center", gap: 6,
                fontSize: 12, color: "var(--accent-600)", fontWeight: 600,
                padding: "8px 0",
              }}>
                <IconChevronDown size={14} /> 크기 · 위치 · 시간 조건 (고급)
              </summary>
              <div style={{ display: "flex", flexDirection: "column", gap: 8, marginTop: 4,
                            paddingLeft: 12, borderLeft: "2px solid var(--accent-100)" }}>
                <CriterionRow label="창 크기">
                  <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                    <select className="input" style={{ width: 80 }}>
                      <option>이하</option><option>이상</option><option>정확히</option>
                    </select>
                    <input className="input mono" defaultValue="500" style={{ width: 80 }} />
                    <span style={{ color: "var(--text-tertiary)" }}>×</span>
                    <input className="input mono" defaultValue="400" style={{ width: 80 }} />
                  </div>
                </CriterionRow>
                <CriterionRow label="위치">
                  <select className="input" style={{ width: 200 }}>
                    <option>화면 어디든</option>
                    <option>우하단에 떠오를 때만</option>
                    <option>중앙에 떠오를 때만</option>
                  </select>
                </CriterionRow>
                <CriterionRow label="시간대">
                  <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                    <input className="input mono" defaultValue="09:00" style={{ width: 80 }} />
                    <span style={{ color: "var(--text-tertiary)" }}>–</span>
                    <input className="input mono" defaultValue="18:00" style={{ width: 80 }} />
                    <span className="badge subtle">평일</span>
                  </div>
                </CriterionRow>
              </div>
            </details>
          </div>
        </div>

        {/* Step 3 — action */}
        <div className="card" style={{ padding: 18 }}>
          <StepHeader n="3" title="이렇게 처리하기" subtitle="이 규칙이 매칭되면 어떻게 할지" />
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10, marginTop: 4 }}>
            <ActionTile selected icon={IconMinimize} title="작게 내리기" desc="작업 표시줄로 보냅니다. 가장 안전합니다." />
            <ActionTile icon={IconEyeOff} title="숨기기" desc="화면에서 숨겨두고 트레이 기록에만 남깁니다." />
            <ActionTile icon={IconLog} title="기록만" desc="아무것도 하지 않고 처리 기록에만 남깁니다." />
            <ActionTile danger icon={IconClose} title="닫기" desc="창을 강제로 닫습니다. 신중하게 사용하세요." />
          </div>

          {/* Danger banner because close is risky */}
          <div style={{
            marginTop: 12,
            display: "flex", gap: 10,
            background: "var(--danger-bg)",
            border: "1px solid color-mix(in oklab, var(--danger) 30%, transparent)",
            borderRadius: 8, padding: "10px 12px",
            color: "oklch(38% 0.12 25)",
            fontSize: 12, lineHeight: 1.5
          }}>
            <IconWarn size={16} style={{ flexShrink: 0, marginTop: 1 }} />
            <div>
              <b>"닫기"는 사용자가 작업 중인 창도 닫을 수 있어요.</b> 시스템 창과 권한 상승 창은 안전 정책에 따라 보호됩니다.
              먼저 <b>기록만</b> 모드로 며칠 지켜본 뒤 바꾸는 것을 권장해요.
            </div>
          </div>
        </div>
      </div>

      {/* Right — preview / summary */}
      <aside style={{ display: "flex", flexDirection: "column", gap: 12 }}>
        <div className="card" style={{ padding: 16, position: "sticky", top: 0 }}>
          <div className="h-eyebrow">미리보기</div>
          <div style={{ fontSize: 13, fontWeight: 600, marginTop: 6 }}>쇼핑몰 광고 팝업 내리기</div>
          <div style={{ marginTop: 10, fontSize: 12, color: "var(--text-secondary)", lineHeight: 1.7 }}>
            다음 조건이 모두 일치하면
            <div style={{ margin: "6px 0", display: "flex", flexWrap: "wrap", gap: 4 }}>
              <span className="chip"><span className="chip-key">제목</span><span className="chip-val">특가 *</span></span>
              <span className="chip"><span className="chip-key">앱</span><span className="chip-val">ShopAssist.exe</span></span>
            </div>
            <ActionBadge action="minimize" /> 합니다.
          </div>
          <div style={{ borderTop: "1px solid var(--stroke-subtle)", marginTop: 12, paddingTop: 10 }}>
            <div className="h-eyebrow">예상 영향</div>
            <div style={{ fontSize: 12, color: "var(--text-secondary)", marginTop: 6 }}>
              최근 7일 기준 <b className="num" style={{ color: "var(--text-primary)" }}>84개</b> 창이 이 규칙에 일치했어요.
            </div>
            <div style={{ display: "flex", alignItems: "flex-end", gap: 2, height: 28, marginTop: 8 }}>
              {[4,6,3,8,5,12,9].map((v,i) => (
                <div key={i} style={{ flex: 1, height: v*2, background: "var(--accent-200)", borderRadius: 1 }} />
              ))}
            </div>
            <div style={{ fontSize: 10, color: "var(--text-tertiary)", marginTop: 4, display: "flex", justifyContent: "space-between" }}>
              <span>월</span><span>화</span><span>수</span><span>목</span><span>금</span><span>토</span><span>일</span>
            </div>
          </div>
          <div style={{ borderTop: "1px solid var(--stroke-subtle)", marginTop: 12, paddingTop: 10 }}>
            <div className="toggle-row" style={{ padding: 0 }}>
              <div className="toggle-text">
                <div className="toggle-title">규칙 활성화</div>
                <div className="toggle-desc">저장 직후 작동을 시작합니다.</div>
              </div>
              <div className="toggle on" />
            </div>
          </div>
        </div>
      </aside>
    </div>
  </div>
);

const StepHeader = ({ n, title, subtitle }) => (
  <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 14 }}>
    <div style={{
      width: 22, height: 22, borderRadius: 6,
      background: "var(--accent-50)", color: "var(--accent-700)",
      display: "grid", placeItems: "center",
      fontSize: 12, fontWeight: 700,
      border: "1px solid var(--accent-100)",
    }}>{n}</div>
    <div>
      <div style={{ fontSize: 14, fontWeight: 600 }}>{title}</div>
      <div style={{ fontSize: 12, color: "var(--text-tertiary)" }}>{subtitle}</div>
    </div>
  </div>
);

const FieldRow = ({ label, hint, children }) => (
  <div style={{ display: "grid", gridTemplateColumns: "120px 1fr", gap: 12, padding: "10px 0",
                borderTop: "1px solid var(--stroke-subtle)" }}>
    <div>
      <div style={{ fontSize: 13, fontWeight: 500 }}>{label}</div>
      {hint && <div style={{ fontSize: 11, color: "var(--text-tertiary)", marginTop: 2 }}>{hint}</div>}
    </div>
    <div>{children}</div>
  </div>
);

const CriterionRow = ({ label, hint, on = false, children }) => (
  <div style={{ display: "grid", gridTemplateColumns: "44px 120px 1fr", alignItems: "center", gap: 10 }}>
    <div className={"toggle sm" + (on ? " on" : "")} />
    <div>
      <div style={{ fontSize: 13, fontWeight: 500, color: on ? "var(--text-primary)" : "var(--text-tertiary)" }}>{label}</div>
      {hint && <div style={{ fontSize: 11, color: "var(--text-tertiary)" }}>{hint}</div>}
    </div>
    <div style={{ opacity: on ? 1 : 0.5 }}>{children}</div>
  </div>
);

const ActionTile = ({ icon: IconC, title, desc, selected, danger }) => (
  <div style={{
    border: "1.5px solid " + (selected ? "var(--accent-500)" : "var(--stroke-default)"),
    background: selected ? "var(--accent-50)" : "var(--surface-panel)",
    borderRadius: 10, padding: 12,
    display: "flex", gap: 10, alignItems: "flex-start",
    cursor: "pointer", position: "relative"
  }}>
    <div style={{
      width: 32, height: 32, borderRadius: 8,
      background: danger ? "var(--danger-bg)" : (selected ? "var(--surface-panel)" : "var(--surface-sunken)"),
      color: danger ? "var(--danger)" : (selected ? "var(--accent-600)" : "var(--text-secondary)"),
      display: "grid", placeItems: "center", flexShrink: 0
    }}>
      <IconC size={16} />
    </div>
    <div style={{ flex: 1 }}>
      <div style={{ fontSize: 13, fontWeight: 600, display: "flex", alignItems: "center", gap: 6 }}>
        {title}
        {danger && <span className="badge action-close" style={{ height: 16, fontSize: 10 }}>위험</span>}
      </div>
      <div style={{ fontSize: 11, color: "var(--text-tertiary)", marginTop: 2, lineHeight: 1.5 }}>{desc}</div>
    </div>
    {selected && (
      <div style={{ position: "absolute", top: 8, right: 8,
                    width: 16, height: 16, borderRadius: "50%",
                    background: "var(--accent-500)", color: "white",
                    display: "grid", placeItems: "center" }}>
        <IconCheck size={10} />
      </div>
    )}
  </div>
);

Object.assign(window, { RuleEditor });
