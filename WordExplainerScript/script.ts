import swal from "sweetalert";

document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("[data-id]").forEach((word) => {
    (word as HTMLElement).addEventListener("click", (event: MouseEvent) => {
      const target = event.target as HTMLElement;
      if (target && target.textContent) {
        swal("Here's the title!", `Word: ${target.textContent}`);
      }
    });
  });
});
