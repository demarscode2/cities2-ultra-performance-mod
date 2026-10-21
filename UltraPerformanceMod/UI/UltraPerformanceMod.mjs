/*! 
 * Cities: Skylines II UI Module
 *
 * Id: UltraPerformanceMod
 * Author: Antigravity
 * Version: 1.3.1
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
const invisibleTrees$ = api.bindValue(Group, "InvisibleTrees", false);
const disableTreeWind$ = api.bindValue(Group, "DisableTreeWind", true);
const waterSimulation$ = api.bindValue(Group, "WaterSimulation", 1);
const autoClearDeadlocks$ = api.bindValue(Group, "AutoClearDeadlocks", false);
const hideNotificationIcons$ = api.bindValue(Group, "HideNotificationIcons", false);

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
        const invisibleTrees = api.useValue(invisibleTrees$);
        const disableTreeWind = api.useValue(disableTreeWind$);
        const waterSimulation = api.useValue(waterSimulation$);
        const autoClearDeadlocks = api.useValue(autoClearDeadlocks$);
        const hideNotificationIcons = api.useValue(hideNotificationIcons$);

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
                                    { label: "Ocultar Todas as \u00C1rvores", sub: "Remove draw calls de folhagem no mapa inteiro", key: "InvisibleTrees", val: invisibleTrees },
                                    { label: "Desativar Vento nas \u00C1rvores", sub: "Elimina anima\u00E7\u00F5es de vento nas copas", key: "DisableTreeWind", val: disableTreeWind },
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
                            // Water Simulation
                            React.createElement(
                                "div",
                                { className: "upm-group-section" },
                                React.createElement("div", { className: "upm-group-title" }, "Simula\u00E7\u00E3o Din\u00E2mica de \u00C1gua"),
                                React.createElement(
                                    "div",
                                    { className: "upm-segmented-bar" },
                                    [
                                        { id: 0, label: "Congelada" },
                                        { id: 1, label: "Otimizada" },
                                        { id: 2, label: "Vanilla" }
                                    ].map((w) =>
                                        React.createElement(
                                            "button",
                                            {
                                                key: w.id,
                                                className: "upm-seg-btn" + (waterSimulation === w.id ? " active" : ""),
                                                onClick: () => api.trigger(Group, "SetWaterMode", w.id)
                                            },
                                            w.label
                                        )
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
                                    React.createElement("b", null, "Simula\u00E7\u00E3o de \u00C1gua:"),
                                    " Em cidades gigantescas, a simula\u00E7\u00E3o de \u00E1gua consome muitos threads da CPU. Usar o modo 'Otimizada' ou 'Congelada' eleva o FPS instantaneamente."
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
