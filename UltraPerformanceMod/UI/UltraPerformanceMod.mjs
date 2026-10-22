/*! 
 * Cities: Skylines II UI Module
 *
 * Id: UltraPerformanceMod
 * Author: Antigravity
 * Version: 1.3.4
 * Dependencies: 
 */

const React = window.React;
const { useState, useEffect, useRef } = React;
const api = window["cs2/api"];
const ui = window["cs2/ui"] || {};

const Group = "UltraPerformanceMod";

// Bindings
const isPanelOpen$ = api.bindValue(Group, "IsPanelOpen", false);
const statusMessage$ = api.bindValue(Group, "StatusMessage", "");
const preset$ = api.bindValue(Group, "Preset", 2);

const invisibleNPCs$ = api.bindValue(Group, "InvisibleNPCs", false);
const invisibleVehicles$ = api.bindValue(Group, "InvisibleVehicles", false);
const invisibleBicycles$ = api.bindValue(Group, "InvisibleBicycles", false);
const invisibleMotorcycles$ = api.bindValue(Group, "InvisibleMotorcycles", false);
const disableTreeWind$ = api.bindValue(Group, "DisableTreeWind", true);
const disableTreeGrowth$ = api.bindValue(Group, "DisableTreeGrowth", true);
const disableHoverOverlay$ = api.bindValue(Group, "DisableHoverOverlay", false);
const optimizeShadows$ = api.bindValue(Group, "OptimizeShadows", true);
const treeLodMode$ = api.bindValue(Group, "TreeLodMode", 0);
const autoEvacuateHomeless$ = api.bindValue(Group, "AutoEvacuateHomeless", true);
const autoClearDeadlocks$ = api.bindValue(Group, "AutoClearDeadlocks", false);
const hideNotificationIcons$ = api.bindValue(Group, "HideNotificationIcons", false);

// UltraCore Telemetry Bindings
const diagFps$ = api.bindValue(Group, "DiagFps", 0);
const diagAvgFps$ = api.bindValue(Group, "DiagAvgFps", 0);
const diag1PercentLow$ = api.bindValue(Group, "Diag1PercentLow", 0);
const diagFrameTime$ = api.bindValue(Group, "DiagFrameTime", 0);
const diagMemoryMb$ = api.bindValue(Group, "DiagMemoryMb", 0);
const diagModCpuMs$ = api.bindValue(Group, "DiagModCpuMs", 0);
const diagLoadState$ = api.bindValue(Group, "DiagLoadState", "Moderado");
const diagProfile$ = api.bindValue(Group, "DiagProfile", 0);
const diagFrameBudgetMs$ = api.bindValue(Group, "DiagFrameBudgetMs", 1.2);
const diagCacheEntries$ = api.bindValue(Group, "DiagCacheEntries", 0);
const diagZeroAllocOps$ = api.bindValue(Group, "DiagZeroAllocOps", 0);

// Event stopper
const stopPropagation = (e) => {
    if (e && e.stopPropagation) {
        e.stopPropagation();
    }
};

// Lightning Icon URL for Toolbar Button
const iconSrc = "coui://ui-mods/images/PerformanceIcon.svg";

// Native SVG Icons for Action Cards (eliminates missing emoji font squares)
const renderCardIcon = (key) => {
    switch (key) {
        case "PurgeTrailers":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#4287f5" },
                React.createElement("path", { d: "M20 8h-3V4H1v13h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4zM6 18.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zm12 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM17 12V9.5h2.5l2 2.5H17z" })
            );
        case "ClearDeadlocks":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#f5a742" },
                React.createElement("path", { d: "M12 2c-3.3 0-6 2.7-6 6v8c0 3.3 2.7 6 6 6s6-2.7 6-6V8c0-3.3-2.7-6-6-6zm0 4c1.1 0 2 .9 2 2s-.9 2-2 2-2-.9-2-2 .9-2 2-2zm0 6c1.1 0 2 .9 2 2s-.9 2-2 2-2-.9-2-2 .9-2 2-2zm0 6c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z" })
            );
        case "ClearAccidents":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#f54242" },
                React.createElement("path", { d: "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z" })
            );
        case "PurgeOutsideTaxis":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#ffd700" },
                React.createElement("path", { d: "M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z" })
            );
        case "PurgeDummyTraffic":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "none", stroke: "#64b5f6", strokeWidth: "2.5", strokeLinecap: "round" },
                React.createElement("path", { d: "M5 12h14M13 6l6 6-6 6" })
            );
        case "PurgeParkedCars":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#42a5f5" },
                React.createElement("path", { d: "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-6 9.5h-3v-3h3c.83 0 1.5.67 1.5 1.5s-.67 1.5-1.5 1.5zM10 6.5h3c1.93 0 3.5 1.57 3.5 3.5s-1.57 3.5-3.5 3.5h-1v4h-2v-11z" })
            );
        case "CleanGarbage":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#66bb6a" },
                React.createElement("path", { d: "M15 4V3H9v1H4v2h1v13c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V6h1V4h-5zm2 15H7V6h10v13zM9 8h2v9H9V8zm4 0h2v9h-2V8z" })
            );
        case "CollectDeceased":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#ab47bc" },
                React.createElement("path", { d: "M19 10.5h-5v-7c0-.83-.67-1.5-1.5-1.5h-1c-.83 0-1.5.67-1.5 1.5v7H5c-.83 0-1.5.67-1.5 1.5v1c0 .83.67 1.5 1.5 1.5h5v9.5h3V14.5h5c.83 0 1.5-.67 1.5-1.5v-1c0-.83-.67-1.5-1.5-1.5z" })
            );
        case "MaintainRoads":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#78909c" },
                React.createElement("path", { d: "M18.1 4H5.9L3.5 20h17l-2.4-16zM11 6h2v3h-2V6zm0 5h2v3h-2v-3zm0 5h2v3h-2v-3z" })
            );
        case "MaintainParks":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#26a69a" },
                React.createElement("path", { d: "M12 2C8.5 2 6 5.5 6 9c0 2.5 1.5 4.7 3.5 5.8L9.5 22h5l0-7.2c2-.9 3.5-3.3 3.5-5.8 0-3.5-2.5-7-6-7z" })
            );
        case "HealCity":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#ef5350" },
                React.createElement("path", { d: "M19 10.5h-5.5V5h-3v5.5H5v3h5.5V19h3v-5.5H19v-3z" })
            );
        case "DeliverMail":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#ffca28" },
                React.createElement("path", { d: "M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z" })
            );
        case "PurgeAnimals":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#ffa726" },
                React.createElement("path", { d: "M12 11c1.66 0 3-1.34 3-3s-1.34-3-3-3-3 1.34-3 3 1.34 3 3 3zm-6 2c1.38 0 2.5-1.12 2.5-2.5S7.38 8 6 8s-2.5 1.12-2.5 2.5S4.62 13 6 13zm12 0c1.38 0 2.5-1.12 2.5-2.5S19.38 8 18 8s-2.5 1.12-2.5 2.5 1.12 2.5 2.5 2.5zm-6 2c-3.11 0-6 2.05-6 4.5 0 1.5 1.5 2.5 3 2.5 1.5 0 2-.5 3-.5s1.5.5 3 .5c1.5 0 3-1 3-2.5 0-2.45-2.89-4.5-6-4.5z" })
            );
        case "CompleteConstruction":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#8d6e63" },
                React.createElement("path", { d: "M12 3L2 12h3v8h14v-8h3L12 3zm1 14h-2v-4h2v4zm0-6h-2V9h2v2z" })
            );
        case "PurgeHomeless":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#e91e63" },
                React.createElement("path", { d: "M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z" })
            );
        case "ClearStuckPedestrians":
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#00e676" },
                React.createElement("path", { d: "M13.5 5.5c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zM9.8 8.9L7 23h2.1l1.8-8 2.1 2v6h2v-7.5l-2.1-2 .6-3C14.8 12 16.8 13 19 13v-2c-1.9 0-3.5-1-4.3-2.4l-1-1.6c-.4-.6-1-1-1.7-1-.3 0-.5.1-.8.1L6 8.3V13h2V9.6l1.8-.7z" })
            );
        default:
            return React.createElement("svg", { viewBox: "0 0 24 24", width: "18rem", height: "18rem", fill: "#4287f5" },
                React.createElement("circle", { cx: "12", cy: "12", r: "8" })
            );
    }
};

// Toolbar Button in GameTopLeft (identical to BetterTransitView & ParkManager)
const ToolbarButton = () => {
    try {
        const isOpen = api.useValue(isPanelOpen$);
        const Tooltip = ui.Tooltip || (({ children }) => children);
        const FloatingButton = ui.FloatingButton || ui.Button;

        return React.createElement(
            Tooltip,
            { tooltip: "Ultra Performance Booster (Ajustes e A\u00E7\u00F5es R\u00E1pidas)" },
            React.createElement(FloatingButton, {
                src: iconSrc,
                selected: isOpen,
                variant: "floating",
                className: "upm-hud-btn" + (isOpen ? " selected" : ""),
                tooltipLabel: "Ultra Performance Booster",
                onSelect: () => api.trigger(Group, "TogglePanel")
            })
        );
    } catch (err) {
        console.error("[UltraPerformanceMod] ToolbarButton render error:", err);
        return null;
    }
};

// Quick Action Card Component
const ActionCard = ({ title, desc, actionKey, isDanger = false }) => {
    return React.createElement(
        "div",
        {
            className: "upm-card" + (isDanger ? " upm-card-danger" : ""),
            onClick: () => api.trigger(Group, "ExecuteQuickAction", actionKey)
        },
        React.createElement(
            "div",
            { className: "upm-card-header" },
            React.createElement("span", { className: "upm-card-icon" }, renderCardIcon(actionKey)),
            React.createElement("span", { className: "upm-card-title" }, title)
        ),
        React.createElement("div", { className: "upm-card-desc" }, desc)
    );
};

// Toggle Switch Component
const ToggleSwitch = ({ checked, onChange }) => {
    return React.createElement(
        "div",
        {
            className: "upm-switch-box" + (checked ? " active" : ""),
            onClick: () => onChange(!checked)
        },
        React.createElement("div", { className: "upm-switch-handle" })
    );
};

// Floating Panel Component (Styled like Better Transit View & Park Manager)
const FloatingPanel = () => {
    try {
        const isOpen = api.useValue(isPanelOpen$);
        const [activeTab, setActiveTab] = useState("actions"); // "actions" | "settings" | "info"

        const statusMsg = api.useValue(statusMessage$);
        const preset = api.useValue(preset$);

        const invisibleNPCs = api.useValue(invisibleNPCs$);
        const invisibleVehicles = api.useValue(invisibleVehicles$);
        const invisibleBicycles = api.useValue(invisibleBicycles$);
        const invisibleMotorcycles = api.useValue(invisibleMotorcycles$);
        const disableTreeWind = api.useValue(disableTreeWind$);
        const disableTreeGrowth = api.useValue(disableTreeGrowth$);
        const disableHoverOverlay = api.useValue(disableHoverOverlay$);
        const optimizeShadows = api.useValue(optimizeShadows$);
        const treeLodMode = api.useValue(treeLodMode$);
        const autoEvacuateHomeless = api.useValue(autoEvacuateHomeless$);
        const autoClearDeadlocks = api.useValue(autoClearDeadlocks$);
        const hideNotificationIcons = api.useValue(hideNotificationIcons$);

        const diagFps = api.useValue(diagFps$);
        const diagAvgFps = api.useValue(diagAvgFps$);
        const diag1PercentLow = api.useValue(diag1PercentLow$);
        const diagFrameTime = api.useValue(diagFrameTime$);
        const diagMemoryMb = api.useValue(diagMemoryMb$);
        const diagModCpuMs = api.useValue(diagModCpuMs$);
        const diagLoadState = api.useValue(diagLoadState$);
        const diagProfile = api.useValue(diagProfile$);
        const diagFrameBudgetMs = api.useValue(diagFrameBudgetMs$);
        const diagCacheEntries = api.useValue(diagCacheEntries$);
        const diagZeroAllocOps = api.useValue(diagZeroAllocOps$);

        if (!isOpen) return null;

        const presetNames = ["Personalizado", "B\u00E1sico", "M\u00E9dio", "M\u00E1ximo"];
        const currentPresetLabel = presetNames[preset] || "Personalizado";

        const Scrollable = ui.Scrollable || (({ children, style }) => React.createElement("div", { style: { overflowY: "auto", ...style } }, children));

        return React.createElement(
            "div",
            {
                className: "upm-window-root",
                onMouseDown: stopPropagation,
                onMouseUp: stopPropagation,
                onClick: stopPropagation,
                onContextMenu: stopPropagation
            },
            React.createElement(
                "div",
                { className: "upm-panel-container" },
                // Header
                React.createElement(
                    "div",
                    { className: "upm-panel-header" },
                    React.createElement(
                        "div",
                        { className: "upm-header-left" },
                        React.createElement("svg", {
                            viewBox: "0 0 24 24",
                            width: "18rem",
                            height: "18rem",
                            fill: "#ffd700",
                            style: { marginRight: "6rem", flexShrink: 0 }
                        },
                            React.createElement("path", { d: "M13 2L3 14h7l-2 8 11-12h-7l2-8z" })
                        ),
                        React.createElement("h2", { className: "upm-title" }, "Ultra Performance Booster"),
                        React.createElement("span", { className: "upm-preset-badge" }, currentPresetLabel)
                    ),
                    React.createElement(
                        "button",
                        {
                            className: "upm-close-button",
                            onClick: () => api.trigger(Group, "TogglePanel"),
                            title: "Fechar"
                        },
                        React.createElement("svg", {
                            viewBox: "0 0 24 24",
                            width: "14rem",
                            height: "14rem",
                            stroke: "currentColor",
                            strokeWidth: "2.5",
                            strokeLinecap: "round",
                            fill: "none"
                        },
                            React.createElement("line", { x1: "18", y1: "6", x2: "6", y2: "18" }),
                            React.createElement("line", { x1: "6", y1: "6", x2: "18", y2: "18" })
                        )
                    )
                ),
                // Tab Navigation Bar
                React.createElement(
                    "div",
                    { className: "upm-tab-bar" },
                    React.createElement(
                        "button",
                        {
                            className: "upm-tab-item" + (activeTab === "actions" ? " active" : ""),
                            onClick: () => setActiveTab("actions")
                        },
                        "A\u00E7\u00F5es R\u00E1pidas"
                    ),
                    React.createElement(
                        "button",
                        {
                            className: "upm-tab-item" + (activeTab === "settings" ? " active" : ""),
                            onClick: () => setActiveTab("settings")
                        },
                        "Ajustes em Tempo Real"
                    ),
                    React.createElement(
                        "button",
                        {
                            className: "upm-tab-item" + (activeTab === "ultracore" ? " active" : ""),
                            onClick: () => setActiveTab("ultracore")
                        },
                        "UltraCore & Telemetria"
                    ),
                    React.createElement(
                        "button",
                        {
                            className: "upm-tab-item" + (activeTab === "info" ? " active" : ""),
                            onClick: () => setActiveTab("info")
                        },
                        "Dicas & Guia"
                    )
                ),
                // Content Body (Wrapped in Scrollable)
                React.createElement(
                    Scrollable,
                    {
                        vertical: true,
                        style: { flex: 1, minHeight: 0, padding: "12rem 14rem", maxHeight: "600rem" }
                    },
                    // TAB 1: QUICK ACTIONS
                    activeTab === "actions" &&
                        React.createElement(
                            React.Fragment,
                            null,
                            // Section: Traffic & Vehicles
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Tr\u00E2nsito, Reboques & Ve\u00EDculos"),
                                React.createElement(
                                    "div",
                                    { className: "upm-grid" },
                                    React.createElement(ActionCard, {
                                        title: "Limpar Trailers, Reboques & Vans",
                                        desc: "Remove carretinhas soltas, trailers intermodais e props de vans",
                                        actionKey: "PurgeTrailers"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Desobstruir Tr\u00E2nsito Preso",
                                        desc: "Desfaz gridlocks e travamentos circulares em cruzamentos",
                                        actionKey: "ClearDeadlocks"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Limpar Acidentes da Pista",
                                        desc: "Desobstrui pistas com batidas e capotamentos",
                                        actionKey: "ClearAccidents"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Limpar T\u00E1xis Externos",
                                        desc: "Remove t\u00E1xis de fora nas conex\u00F5es regionais",
                                        actionKey: "PurgeOutsideTaxis"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Limpar Tr\u00E1fego Falso",
                                        desc: "Elimina ve\u00EDculos de passagem fict\u00EDcios",
                                        actionKey: "PurgeDummyTraffic"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Limpar Vagas Abandonadas",
                                        desc: "Desocupa estacionamentos e garagens",
                                        actionKey: "PurgeParkedCars"
                                    })
                                )
                            ),
                            // Section: Public Services
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Servi\u00E7os P\u00FAblicos Instant\u00E2neos"),
                                React.createElement(
                                    "div",
                                    { className: "upm-grid" },
                                    React.createElement(ActionCard, {
                                        title: "Coletar Todo Lixo",
                                        desc: "Esvazia lixo de todos os im\u00F3veis da cidade",
                                        actionKey: "CleanGarbage"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Funer\u00E1ria Autom\u00E1tica",
                                        desc: "Recolhe \u00F3bitos pendentes sem ve\u00EDculos",
                                        actionKey: "CollectDeceased"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Manuten\u00E7\u00E3o de Rodovias",
                                        desc: "Repara 100% das vias e asfalto da cidade",
                                        actionKey: "MaintainRoads"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Manuten\u00E7\u00E3o de Parques",
                                        desc: "Revitaliza o estado de todos os parques e pra\u00E7as",
                                        actionKey: "MaintainParks"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Socorrer Cidade & Sa\u00FAde",
                                        desc: "Cura doentes e atende chamados de emerg\u00EAncia",
                                        actionKey: "HealCity"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Entregar Correspond\u00EAncias",
                                        desc: "Zera cartas e encomendas acumuladas",
                                        actionKey: "DeliverMail"
                                    })
                                )
                            ),
                            // Section: Nature & Environment
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Natureza, Animais & Obras"),
                                React.createElement(
                                    "div",
                                    { className: "upm-grid" },
                                    React.createElement(ActionCard, {
                                        title: "Remover Animais Soltos",
                                        desc: "Expurga c\u00E3es e animais selvagens para poupar CPU",
                                        actionKey: "PurgeAnimals"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Concluir Todas as Obras",
                                        desc: "Finaliza instantaneamente pr\u00E9dios em constru\u00E7\u00E3o",
                                        actionKey: "CompleteConstruction"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Despachar Sem-Teto Presos",
                                        desc: "Zera fam\u00EDlias presas em loops de pathfinding (alivia CPU)",
                                        actionKey: "PurgeHomeless"
                                    }),
                                    React.createElement(ActionCard, {
                                        title: "Desobstruir Pedestres Travados",
                                        desc: "Desbloqueia pedestres travados em cal\u00E7adas e esta\u00E7\u00F5es",
                                        actionKey: "ClearStuckPedestrians"
                                    })
                                )
                            )
                        ),
                    // TAB 2: LIVE SETTINGS
                    activeTab === "settings" &&
                        React.createElement(
                            React.Fragment,
                            null,
                            // Performance Presets
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Predefini\u00E7\u00F5es de Desempenho"),
                                React.createElement(
                                    "div",
                                    { className: "upm-preset-buttons" },
                                    [
                                        { id: 1, label: "B\u00E1sico" },
                                        { id: 2, label: "M\u00E9dio" },
                                        { id: 3, label: "M\u00E1ximo" }
                                    ].map((p) =>
                                        React.createElement(
                                            "button",
                                            {
                                                key: p.id,
                                                className: "upm-preset-btn" + (preset === p.id ? " active" : ""),
                                                onClick: () => api.trigger(Group, "SelectPreset", p.id)
                                            },
                                            p.label
                                        )
                                    )
                                )
                            ),
                            // Entity Visibility
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Visibilidade de Entidades (GPU)"),
                                [
                                    { label: "Ocultar Cidad\u00E3os (NPCs)", sub: "Desativa anima\u00E7\u00F5es e renderiza\u00E7\u00E3o de pedestres", key: "InvisibleNPCs", val: invisibleNPCs },
                                    { label: "Ocultar Ve\u00EDculos Gerais", sub: "Oculta carros, caminh\u00F5es e transportes", key: "InvisibleVehicles", val: invisibleVehicles },
                                    { label: "Ocultar Bicicletas", sub: "Oculta ciclistas individualmente", key: "InvisibleBicycles", val: invisibleBicycles },
                                    { label: "Ocultar Motocicletas", sub: "Oculta motociclistas individualmente", key: "InvisibleMotorcycles", val: invisibleMotorcycles },
                                    { label: "Ocultar Borda Azul ao Passar Mouse", sub: "Remove o contorno / highlight azul ao pairar sobre itens", key: "DisableHoverOverlay", val: disableHoverOverlay },
                                    { label: "Desativar Vento nas \u00C1rvores", sub: "Elimina oscila\u00E7\u00E3o e c\u00E1lculos de vento nas copas", key: "DisableTreeWind", val: disableTreeWind },
                                    { label: "Desativar Crescimento de \u00C1rvores", sub: "Pausa envelhecimento de \u00E1rvores poupando CPU", key: "DisableTreeGrowth", val: disableTreeGrowth },
                                    { label: "Ocultar \u00CDcones de Alerta", sub: "Oculta bal\u00F5es de falta de clientes, aluguel, etc.", key: "HideNotificationIcons", val: hideNotificationIcons },
                                    { label: "Desobstru\u00E7\u00E3o Autom\u00E1tica de Tr\u00E2nsito", sub: "Despacha automaticamente carros em impasse circular", key: "AutoClearDeadlocks", val: autoClearDeadlocks }
                                ].map((item) =>
                                    React.createElement(
                                        "div",
                                        { className: "upm-row", key: item.key },
                                        React.createElement(
                                            "div",
                                            { className: "upm-row-text" },
                                            React.createElement("span", { className: "upm-row-label" }, item.label),
                                            React.createElement("span", { className: "upm-row-sub" }, item.sub)
                                        ),
                                        React.createElement(ToggleSwitch, {
                                            checked: item.val,
                                            onChange: (val) => api.trigger(Group, "ToggleSetting", item.key, val)
                                        })
                                    )
                                )
                            ),
                            // Advanced GPU Graphics Optimization
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Otimiza\u00E7\u00F5es Gr\u00E1ficas Avan\u00E7adas (GPU)"),
                                [
                                    { label: "Otimizar Sombras & Dist\u00E2ncia HDRP", sub: "Limita sombras a 450m e filtra micro-props (salto brutal de FPS)", key: "OptimizeShadows", val: optimizeShadows }
                                ].map((item) =>
                                    React.createElement(
                                        "div",
                                        { className: "upm-row", key: item.key },
                                        React.createElement(
                                            "div",
                                            { className: "upm-row-text" },
                                            React.createElement("span", { className: "upm-row-label" }, item.label),
                                            React.createElement("span", { className: "upm-row-sub" }, item.sub)
                                        ),
                                        React.createElement(ToggleSwitch, {
                                            checked: item.val,
                                            onChange: (val) => api.trigger(Group, "ToggleSetting", item.key, val)
                                        })
                                    )
                                )
                            ),
                            // Section: Experimental Foliage & Mesh LOD Control
                            React.createElement(
                                "div",
                                { className: "upm-group-section upm-experimental-section" },
                                React.createElement(
                                    "div",
                                    { className: "upm-experimental-header" },
                                    React.createElement("span", { className: "upm-badge-experimental" }, "EXPERIMENTAL"),
                                    React.createElement("div", { className: "upm-group-title", style: { margin: 0 } }, "Controle de LOD & \u00C1rvores Distantes")
                                ),
                                React.createElement(
                                    "div",
                                    { className: "upm-experimental-desc" },
                                    "Fun\u00E7\u00E3o Experimental: Controla a dist\u00E2ncia de renderiza\u00E7\u00E3o de folhagem e \u00E1rvores. Em n\u00EDveis agressivos, \u00E1rvores distantes deixam de ser processadas e surgem suavemente conforme a c\u00E2mera se aproxima, gerando grande al\u00EDvio de draw calls e v\u00E9rtices na GPU."
                                ),
                                React.createElement(
                                    "div",
                                    { className: "upm-lod-buttons" },
                                    [
                                        { id: 0, label: "Padr\u00E3o", sub: "100% Nativo" },
                                        { id: 1, label: "Moderado", sub: "~65% Horizonte" },
                                        { id: 2, label: "Agressivo", sub: "~40% C\u00E2mera" },
                                        { id: 3, label: "Extremo", sub: "~20% Culling M\u00E1x" }
                                    ].map((m) =>
                                        React.createElement(
                                            "button",
                                            {
                                                key: m.id,
                                                className: "upm-lod-btn" + (treeLodMode === m.id ? " active" : ""),
                                                onClick: () => api.trigger(Group, "SetTreeLodMode", m.id)
                                            },
                                            React.createElement("span", { className: "upm-lod-btn-title" }, m.label),
                                            React.createElement("span", { className: "upm-lod-btn-sub" }, m.sub)
                                        )
                                    )
                                ),
                                React.createElement(
                                    "div",
                                    { className: "upm-lod-status" },
                                    React.createElement("span", { className: "upm-lod-status-label" }, "Efeito Atual: "),
                                    React.createElement(
                                        "span",
                                        { className: "upm-lod-status-val" },
                                        treeLodMode === 0
                                            ? "Padr\u00E3o nativo (todas as \u00E1rvores renderizadas normalmente no horizonte)"
                                            : treeLodMode === 1
                                            ? "Moderado (florestas no horizonte reduzidas - ganho suave de FPS)"
                                            : treeLodMode === 2
                                            ? "Agressivo (\u00E1rvores s\u00F3 aparecem perto da c\u00E2mera - salto brutal de FPS)"
                                            : "Extremo (culling m\u00E1ximo - renderiza apenas folhagem colada \u00E0 c\u00E2mera)"
                                    )
                                )
                            ),
                            // City Simulation CPU Automations
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Automa\u00E7\u00F5es Urbanas (Al\u00EDvio de CPU)"),
                                [
                                    { label: "Evacua\u00E7\u00E3o Autom\u00E1tica de Sem-Teto", sub: "Despacha periodicamente sem-teto que travam filas de pathfinding", key: "AutoEvacuateHomeless", val: autoEvacuateHomeless }
                                ].map((item) =>
                                    React.createElement(
                                        "div",
                                        { className: "upm-row", key: item.key },
                                        React.createElement(
                                            "div",
                                            { className: "upm-row-text" },
                                            React.createElement("span", { className: "upm-row-label" }, item.label),
                                            React.createElement("span", { className: "upm-row-sub" }, item.sub)
                                        ),
                                        React.createElement(ToggleSwitch, {
                                            checked: item.val,
                                            onChange: (val) => api.trigger(Group, "ToggleSetting", item.key, val)
                                        })
                                    )
                                )
                            )
                        ),
                    // TAB: ULTRACORE & DIAGNOSTICS
                    activeTab === "ultracore" &&
                        React.createElement(
                            React.Fragment,
                            null,
                            // Section: Real-Time Telemetry
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Telemetria de Quadros & Simula\u00E7\u00E3o (Tempo Real)"),
                                React.createElement(
                                    "div",
                                    { className: "upm-telemetry-grid" },
                                    React.createElement(
                                        "div",
                                        { className: "upm-metric-card" },
                                        React.createElement("div", { className: "upm-metric-title" }, "FPS Instant\u00E2neo"),
                                        React.createElement(
                                            "div",
                                            { className: "upm-metric-val " + (diagFps >= 45 ? "upm-metric-good" : (diagFps >= 25 ? "upm-metric-warn" : "upm-metric-bad")) },
                                            (diagFps > 0 ? diagFps.toFixed(1) : "--")
                                        )
                                    ),
                                    React.createElement(
                                        "div",
                                        { className: "upm-metric-card" },
                                        React.createElement("div", { className: "upm-metric-title" }, "FPS M\u00E9dio (120q)"),
                                        React.createElement(
                                            "div",
                                            { className: "upm-metric-val " + (diagAvgFps >= 45 ? "upm-metric-good" : (diagAvgFps >= 25 ? "upm-metric-warn" : "upm-metric-bad")) },
                                            (diagAvgFps > 0 ? diagAvgFps.toFixed(1) : "--")
                                        )
                                    ),
                                    React.createElement(
                                        "div",
                                        { className: "upm-metric-card" },
                                        React.createElement("div", { className: "upm-metric-title" }, "1% Low FPS"),
                                        React.createElement(
                                            "div",
                                            { className: "upm-metric-val " + (diag1PercentLow >= 30 ? "upm-metric-good" : (diag1PercentLow >= 18 ? "upm-metric-warn" : "upm-metric-bad")) },
                                            (diag1PercentLow > 0 ? diag1PercentLow.toFixed(1) : "--")
                                        )
                                    ),
                                    React.createElement(
                                        "div",
                                        { className: "upm-metric-card" },
                                        React.createElement("div", { className: "upm-metric-title" }, "Tempo de Quadro"),
                                        React.createElement(
                                            "div",
                                            { className: "upm-metric-val" },
                                            (diagFrameTime > 0 ? diagFrameTime.toFixed(1) : "--"),
                                            React.createElement("span", { className: "upm-metric-unit" }, "ms")
                                        )
                                    ),
                                    React.createElement(
                                        "div",
                                        { className: "upm-metric-card" },
                                        React.createElement("div", { className: "upm-metric-title" }, "Mem\u00F3ria GC"),
                                        React.createElement(
                                            "div",
                                            { className: "upm-metric-val" },
                                            (diagMemoryMb > 0 ? diagMemoryMb.toFixed(0) : "--"),
                                            React.createElement("span", { className: "upm-metric-unit" }, "MB")
                                        )
                                    ),
                                    React.createElement(
                                        "div",
                                        { className: "upm-metric-card" },
                                        React.createElement("div", { className: "upm-metric-title" }, "CPU Mod"),
                                        React.createElement(
                                            "div",
                                            { className: "upm-metric-val upm-metric-good" },
                                            (diagModCpuMs >= 0 ? diagModCpuMs.toFixed(2) : "--"),
                                            React.createElement("span", { className: "upm-metric-unit" }, "ms")
                                        )
                                    )
                                )
                            ),
                            // Section: Auto-Tuner & Adaptive Budgets
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Auto-Tuner Adaptativo da Simula\u00E7\u00E3o"),
                                React.createElement(
                                    "div",
                                    { className: "upm-status-row" },
                                    React.createElement("span", { className: "upm-status-label" }, "Estado de Carga da CPU:"),
                                    React.createElement("span", { className: "upm-status-val" }, diagLoadState || "Normal")
                                ),
                                React.createElement(
                                    "div",
                                    { className: "upm-status-row" },
                                    React.createElement("span", { className: "upm-status-label" }, "Or\u00E7amento por Quadro (Budget):"),
                                    React.createElement("span", { className: "upm-status-val" }, (diagFrameBudgetMs ? diagFrameBudgetMs.toFixed(2) : "1.20") + " ms")
                                ),
                                React.createElement("div", { style: { marginTop: "8rem", marginBottom: "4rem", fontSize: "12rem", color: "#8fa3b8" } }, "Perfil de Execu\u00E7\u00E3o do Auto-Tuner:"),
                                React.createElement(
                                    "div",
                                    { className: "upm-segmented-bar" },
                                    [
                                        { label: "Auto (Recomendado)", idx: 0 },
                                        { label: "Conservador", idx: 1 },
                                        { label: "Equilibrado", idx: 2 },
                                        { label: "Agressivo", idx: 3 }
                                    ].map((p) =>
                                        React.createElement(
                                            "button",
                                            {
                                                key: p.idx,
                                                className: "upm-seg-btn" + (diagProfile === p.idx ? " active" : ""),
                                                onClick: () => api.trigger(Group, "SetAutoTunerProfile", p.idx)
                                            },
                                            p.label
                                        )
                                    )
                                )
                            ),
                            // Section: Smart Cache & Zero-Alloc Memory
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Smart Cache & Otimiza\u00E7\u00F5es Zero-Alloc"),
                                React.createElement(
                                    "div",
                                    { className: "upm-status-row" },
                                    React.createElement("span", { className: "upm-status-label" }, "Cadeias de Bloqueio em Cache:"),
                                    React.createElement("span", { className: "upm-status-val" }, (diagCacheEntries || 0) + " entradas")
                                ),
                                React.createElement(
                                    "div",
                                    { className: "upm-status-row" },
                                    React.createElement("span", { className: "upm-status-label" }, "A\u00E7\u00F5es Zero-Alloc (ECS Direto):"),
                                    React.createElement("span", { className: "upm-status-val upm-metric-good" }, (diagZeroAllocOps || 0) + " opera\u00E7\u00F5es")
                                ),
                                React.createElement(
                                    "div",
                                    { style: { marginTop: "10rem", display: "flex", justifyContent: "flex-end" } },
                                    React.createElement(
                                        "button",
                                        {
                                            className: "upm-btn-primary",
                                            onClick: () => api.trigger(Group, "ClearSmartCache")
                                        },
                                        React.createElement("svg", {
                                            viewBox: "0 0 24 24",
                                            width: "14rem",
                                            height: "14rem",
                                            fill: "currentColor"
                                        },
                                            React.createElement("path", { d: "M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z" })
                                        ),
                                        "Esvaziar Smart Cache"
                                    )
                                )
                            )
                        ),
                    // TAB 3: INFO & TIPS
                    activeTab === "info" &&
                        React.createElement(
                            React.Fragment,
                            null,
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Otimiza\u00E7\u00E3o para Megal\u00F3poles (700.000+ hab)"),
                                React.createElement(
                                    "div",
                                    { style: { fontSize: "14rem", color: "#b5c7d8", lineHeight: "1.6" } },
                                    "\u2022 ",
                                    React.createElement("b", null, "Tr\u00E2nsito Realista:"),
                                    " Carros em sem\u00E1foros, faixas e embarques n\u00E3o somem mais. Apenas travamentos circulares (gridlock) ou carros com reboque solto s\u00E3o corrigidos.",
                                    React.createElement("br"),
                                    React.createElement("br"),
                                    "\u2022 ",
                                    React.createElement("b", null, "Trailers & Props de Vans:"),
                                    " Na aba A\u00E7\u00F5es R\u00E1pidas, use o bot\u00E3o 'Limpar Trailers, Reboques & Vans' para eliminar containers, reboques e vans decorativas que estejam sobrecarregando o mapa.",
                                    React.createElement("br"),
                                    React.createElement("br"),
                                    "\u2022 ",
                                    React.createElement("b", null, "Servi\u00E7os Autom\u00E1ticos:"),
                                    " Lixo, mortos, manuten\u00E7\u00E3o e emerg\u00EAncias podem ser mantidos no modo autom\u00E1tico para eliminar dezenas de milhares de ve\u00EDculos lentos da malha vi\u00E1ria.",
                                    React.createElement("br"),
                                    React.createElement("br"),
                                    "\u2022 ",
                                    React.createElement("b", null, "Sem-Teto & Pathfinding:"),
                                    " Em grandes cidades, cidad\u00E3os sem abrigo entram em loops infinitos tentando achar parques ou sa\u00EDdas. O bot\u00E3o 'Despachar Sem-Teto' libera milhares de threads da CPU instantaneamente.",
                                    React.createElement("br"),
                                    React.createElement("br"),
                                    "\u2022 ",
                                    React.createElement("b", null, "Borda Azul / Highlight:"),
                                    " O mod permite desativar a borda azul ao passar o mouse em objetos, poupando passos inteiros de p\u00F3s-processamento da GPU."
                                )
                            )
                        )
                ),
                // Footer Status Message
                React.createElement(
                    "div",
                    { className: "upm-footer-status" + (statusMsg ? " highlight" : "") },
                    statusMsg || "Pronto. Selecione uma a\u00E7\u00E3o r\u00E1pida ou ajuste as op\u00E7\u00F5es de desempenho."
                )
            )
        );
    } catch (err) {
        console.error("[UltraPerformanceMod] FloatingPanel render error:", err);
        return null;
    }
};

// Module Entrypoint for Cities: Skylines II Cohtml Engine
const registerMod = (moduleQueue) => {
    try {
        console.log("[UltraPerformanceMod] Registering UI components in GameTopLeft and Game...");
        moduleQueue.append("GameTopLeft", ToolbarButton);
        moduleQueue.append("Game", FloatingPanel);
        console.log("[UltraPerformanceMod] UI registered successfully!");
    } catch (err) {
        console.error("[UltraPerformanceMod] UI Registration failed:", err);
    }
};

export default registerMod;
export const hasCSS = true;
