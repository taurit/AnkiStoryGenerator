import tippy from "tippy.js";
import "tippy.js/dist/tippy.css";
import "./custom.css";

document.addEventListener("DOMContentLoaded", () => {
  tippy("[data-id]", {
    allowHTML: true,
    arrow: false,
    animation: "none",
    theme: "myCustomTheme",
    content(reference) {
      const tooltip = reference.getAttribute("data-tooltip");
      return tooltip ? tooltip : "[data-tooltip attribute is missing]";
    },
  });
});
