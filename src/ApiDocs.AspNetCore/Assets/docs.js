/* ApiDocs.AspNetCore docs portal — tiny chrome helpers (no dependencies).
   Copy-to-clipboard on code blocks + the mobile sidebar toggle. */
(function () {
  "use strict";

  document.querySelectorAll("pre").forEach(function (pre) {
    var btn = document.createElement("button");
    btn.type = "button";
    btn.className = "copy-btn";
    btn.textContent = "Copy";
    btn.setAttribute("aria-label", "Copy code to clipboard");
    btn.addEventListener("click", function () {
      var code = pre.querySelector("code");
      var text = (code ? code.innerText : pre.innerText).replace(/\n$/, "");
      navigator.clipboard.writeText(text).then(function () {
        btn.textContent = "Copied";
        btn.classList.add("copied");
        window.setTimeout(function () {
          btn.textContent = "Copy";
          btn.classList.remove("copied");
        }, 1600);
      });
    });
    pre.appendChild(btn);
  });

  var sidebar = document.querySelector(".sidebar");
  var menuBtn = document.querySelector(".menu-btn");
  var scrim = document.querySelector(".scrim");

  function setExpanded(open) {
    if (menuBtn) menuBtn.setAttribute("aria-expanded", open ? "true" : "false");
  }
  function closeNav() {
    if (sidebar) sidebar.classList.remove("open");
    if (scrim) scrim.classList.remove("show");
    setExpanded(false);
  }
  if (menuBtn && sidebar) {
    menuBtn.addEventListener("click", function () {
      var open = sidebar.classList.toggle("open");
      if (scrim) scrim.classList.toggle("show", open);
      setExpanded(open);
    });
  }
  if (scrim) scrim.addEventListener("click", closeNav);
  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape") closeNav();
  });
})();
