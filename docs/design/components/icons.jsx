/* Icon set — line icons, 18px viewBox 24, 1.6 stroke
   Original, simple geometry — no third-party icon set lifted. */

const Icon = ({ d, size = 18, stroke = 1.6, fill = "none" }) => (
  <svg width={size} height={size} viewBox="0 0 24 24"
       fill={fill} stroke="currentColor" strokeWidth={stroke}
       strokeLinecap="round" strokeLinejoin="round"
       style={{ flexShrink: 0 }}>
    {d}
  </svg>
);

const IconHome = (p) => (
  <Icon {...p} d={<>
    <path d="M3.5 10.5 12 4l8.5 6.5" />
    <path d="M5.5 9.8V20h13V9.8" />
    <path d="M10 20v-5h4v5" />
  </>} />
);
const IconRules = (p) => (
  <Icon {...p} d={<>
    <rect x="3.5" y="4.5" width="17" height="15" rx="2.5" />
    <path d="M7 9h10M7 13h7M7 17h5" />
  </>} />
);
const IconWindow = (p) => (
  <Icon {...p} d={<>
    <rect x="3.5" y="4.5" width="17" height="15" rx="2" />
    <path d="M3.5 9h17" />
    <circle cx="6.5" cy="6.7" r="0.6" fill="currentColor" stroke="none"/>
    <circle cx="8.7" cy="6.7" r="0.6" fill="currentColor" stroke="none"/>
  </>} />
);
const IconHistory = (p) => (
  <Icon {...p} d={<>
    <path d="M4 12a8 8 0 1 0 2.6-5.9" />
    <path d="M4 4v3.5h3.5" />
    <path d="M12 8v4.5l3 1.8" />
  </>} />
);
const IconSettings = (p) => (
  <Icon {...p} d={<>
    <circle cx="12" cy="12" r="3" />
    <path d="M19.4 13.6a7.6 7.6 0 0 0 0-3.2l2-1.5-1.8-3.1-2.3.8a7.7 7.7 0 0 0-2.8-1.6L14 2h-4l-.5 2.6a7.7 7.7 0 0 0-2.8 1.6l-2.3-.8L2.6 8.5l2 1.5a7.6 7.6 0 0 0 0 3.2l-2 1.5 1.8 3.1 2.3-.8a7.7 7.7 0 0 0 2.8 1.6L10 22h4l.5-2.6a7.7 7.7 0 0 0 2.8-1.6l2.3.8 1.8-3.1z" />
  </>} />
);
const IconPause = (p) => (
  <Icon {...p} d={<>
    <rect x="6" y="5" width="4" height="14" rx="1" fill="currentColor" stroke="none"/>
    <rect x="14" y="5" width="4" height="14" rx="1" fill="currentColor" stroke="none"/>
  </>} />
);
const IconPlay = (p) => (
  <Icon {...p} d={<>
    <path d="M7 5v14l12-7z" fill="currentColor" stroke="none"/>
  </>} />
);
const IconShield = (p) => (
  <Icon {...p} d={<>
    <path d="M12 3 5 5.5v6.7c0 4.4 3 7.6 7 8.8 4-1.2 7-4.4 7-8.8V5.5z" />
    <path d="M9 12.5l2.2 2.2L15.5 10" />
  </>} />
);
const IconPlus = (p) => (
  <Icon {...p} d={<><path d="M12 5v14M5 12h14"/></>} />
);
const IconSearch = (p) => (
  <Icon {...p} d={<>
    <circle cx="11" cy="11" r="6" />
    <path d="m20 20-3.5-3.5" />
  </>} />
);
const IconFilter = (p) => (
  <Icon {...p} d={<>
    <path d="M4 5h16l-6 8v6l-4-2v-4z" />
  </>} />
);
const IconTarget = (p) => (
  <Icon {...p} d={<>
    <circle cx="12" cy="12" r="8" />
    <circle cx="12" cy="12" r="3" />
    <path d="M12 2v3M12 19v3M22 12h-3M5 12H2" />
  </>} />
);
const IconChevron = (p) => (
  <Icon {...p} d={<><path d="m9 6 6 6-6 6"/></>} />
);
const IconChevronDown = (p) => (
  <Icon {...p} d={<><path d="m6 9 6 6 6-6"/></>} />
);
const IconMore = (p) => (
  <Icon {...p} d={<>
    <circle cx="6" cy="12" r="1.4" fill="currentColor" stroke="none"/>
    <circle cx="12" cy="12" r="1.4" fill="currentColor" stroke="none"/>
    <circle cx="18" cy="12" r="1.4" fill="currentColor" stroke="none"/>
  </>} />
);
const IconEdit = (p) => (
  <Icon {...p} d={<>
    <path d="M4 20h4l10.5-10.5-4-4L4 16z" />
    <path d="m14 5.5 4 4" />
  </>} />
);
const IconTrash = (p) => (
  <Icon {...p} d={<>
    <path d="M5 7h14M9 7V4.5h6V7M7 7l1 13h8l1-13" />
  </>} />
);
const IconClose = (p) => (
  <Icon {...p} d={<><path d="M6 6l12 12M18 6 6 18"/></>} />
);
const IconMinimize = (p) => (
  <Icon {...p} d={<><path d="M5 19h14"/></>} />
);
const IconMaximize = (p) => (
  <Icon {...p} d={<><rect x="5" y="5" width="14" height="14" rx="1"/></>} />
);
const IconWarn = (p) => (
  <Icon {...p} d={<>
    <path d="M12 3 2.5 20h19z" />
    <path d="M12 10v4M12 17v.5" />
  </>} />
);
const IconInfo = (p) => (
  <Icon {...p} d={<>
    <circle cx="12" cy="12" r="9" />
    <path d="M12 11v6M12 7.5v.5" />
  </>} />
);
const IconCheck = (p) => (
  <Icon {...p} d={<><path d="m5 12.5 4.5 4.5L19 7"/></>} />
);
const IconEye = (p) => (
  <Icon {...p} d={<>
    <path d="M2.5 12s3.5-7 9.5-7 9.5 7 9.5 7-3.5 7-9.5 7-9.5-7-9.5-7z" />
    <circle cx="12" cy="12" r="2.6" />
  </>} />
);
const IconEyeOff = (p) => (
  <Icon {...p} d={<>
    <path d="M3 3l18 18" />
    <path d="M10.5 6.2A10 10 0 0 1 12 6c6 0 9.5 6 9.5 6a16 16 0 0 1-3.4 4.1" />
    <path d="M6.2 7.6C3.7 9.4 2.5 12 2.5 12s3.5 6 9.5 6c1.4 0 2.6-.3 3.7-.8" />
    <path d="M9.9 10a3 3 0 0 0 4.1 4.1" />
  </>} />
);
const IconLog = (p) => (
  <Icon {...p} d={<>
    <path d="M6 4h9l4 4v12H6z" />
    <path d="M14 4v5h5" />
    <path d="M9 13h7M9 16.5h5" />
  </>} />
);
const IconCamera = (p) => (
  <Icon {...p} d={<>
    <path d="M4 8h3.5l1.5-2h6l1.5 2H20v10H4z" />
    <circle cx="12" cy="13" r="3.2" />
  </>} />
);
const IconClock = (p) => (
  <Icon {...p} d={<>
    <circle cx="12" cy="12" r="8.5" />
    <path d="M12 7v5.2l3.4 2.1" />
  </>} />
);
const IconLayers = (p) => (
  <Icon {...p} d={<>
    <path d="m12 3 9 5-9 5-9-5z" />
    <path d="m3 13 9 5 9-5" />
  </>} />
);
const IconArrow = (p) => (
  <Icon {...p} d={<><path d="M5 12h14M13 6l6 6-6 6"/></>} />
);
const IconSpark = (p) => (
  <Icon {...p} d={<>
    <path d="M12 3v4M12 17v4M3 12h4M17 12h4M5.5 5.5l2.8 2.8M15.7 15.7l2.8 2.8M5.5 18.5l2.8-2.8M15.7 8.3l2.8-2.8" />
  </>} />
);
const IconTrayLogo = ({ size = 16 }) => (
  <div style={{
    width: size, height: size, borderRadius: 4,
    background: "linear-gradient(135deg, var(--accent-500), var(--accent-700))",
    display: "grid", placeItems: "center", position: "relative", flexShrink: 0
  }}>
    <div style={{ width: size*0.56, height: size*0.44, borderRadius: 1.5, background: "white" }} />
    <div style={{ position: "absolute", bottom: 2, right: 2, width: size*0.25, height: size*0.25, background: "white", borderRadius: 1 }} />
  </div>
);

Object.assign(window, {
  Icon,
  IconHome, IconRules, IconWindow, IconHistory, IconSettings,
  IconPause, IconPlay, IconShield, IconPlus, IconSearch, IconFilter,
  IconTarget, IconChevron, IconChevronDown, IconMore, IconEdit, IconTrash,
  IconClose, IconMinimize, IconMaximize, IconWarn, IconInfo, IconCheck,
  IconEye, IconEyeOff, IconLog, IconCamera, IconClock, IconLayers, IconArrow, IconSpark,
  IconTrayLogo,
});
