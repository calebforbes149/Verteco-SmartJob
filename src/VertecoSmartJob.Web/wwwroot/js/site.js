(function () {
  "use strict";

  const hamburger = document.getElementById("hamburger-btn");
  const sidebar = document.getElementById("sidebar");
  const scrim = document.getElementById("sidebar-scrim");

  function closeSidebar() {
    sidebar?.classList.remove("open");
    scrim?.classList.remove("show");
    hamburger?.setAttribute("aria-expanded", "false");
  }

  if (hamburger && sidebar && scrim) {
    hamburger.addEventListener("click", () => {
      const isOpen = sidebar.classList.toggle("open");
      scrim.classList.toggle("show", isOpen);
      hamburger.setAttribute("aria-expanded", String(isOpen));
    });
    scrim.addEventListener("click", closeSidebar);
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") closeSidebar();
    });
  }

  const toastStack = document.getElementById("toast-stack");
  if (toastStack) {
    const toast = toastStack.dataset.toast;
    const error = toastStack.dataset.error;
    if (toast) showToast(toast, false);
    if (error) showToast(error, true);
  }

  function showToast(message, isError) {
    const el = document.createElement("div");
    el.className = "toast";
    if (isError) el.style.background = "var(--danger)";
    el.textContent = message;
    toastStack.appendChild(el);
    setTimeout(() => el.remove(), 5000);
  }

  const customerSelect = document.getElementById("customer-select");
  const siteSelect = document.getElementById("site-select");
  const newSiteBtn = document.getElementById("new-site-btn");

  async function loadSitesForCustomer(customerId, selectSiteId) {
    if (!customerId) {
      siteSelect.innerHTML = '<option value="">Select a customer first…</option>';
      siteSelect.disabled = true;
      newSiteBtn.disabled = true;
      return;
    }
    siteSelect.disabled = true;
    siteSelect.innerHTML = '<option value="">Loading sites…</option>';
    try {
      const response = await fetch(`${window.smartjob.sitesForCustomerUrl}?customerId=${encodeURIComponent(customerId)}`);
      const sites = await response.json();
      siteSelect.innerHTML = '<option value="">Select a site…</option>';
      for (const site of sites) {
        const opt = document.createElement("option");
        opt.value = site.id;
        opt.textContent = site.name;
        if (selectSiteId && site.id === selectSiteId) opt.selected = true;
        siteSelect.appendChild(opt);
      }
      siteSelect.disabled = false;
      newSiteBtn.disabled = false;
    } catch {
      siteSelect.innerHTML = '<option value="">Couldn\'t load sites — try again</option>';
    }
  }

  if (customerSelect && siteSelect) {
    customerSelect.addEventListener("change", () => loadSitesForCustomer(customerSelect.value, null));

    if (customerSelect.value) {
      loadSitesForCustomer(customerSelect.value, window.smartjob.initialSiteId);
    }
  }

  const newCustomerBtn = document.getElementById("new-customer-btn");
  const newCustomerPanel = document.getElementById("new-customer-panel");
  const ncSaveBtn = document.getElementById("nc-save-btn");
  const ncCancelBtn = document.getElementById("nc-cancel-btn");
  const ncError = document.getElementById("nc-error");

  newCustomerBtn?.addEventListener("click", () => {
    newCustomerPanel.hidden = !newCustomerPanel.hidden;
  });
  ncCancelBtn?.addEventListener("click", () => {
    newCustomerPanel.hidden = true;
    ncError.textContent = "";
  });
  ncSaveBtn?.addEventListener("click", async () => {
    ncError.textContent = "";
    const body = new URLSearchParams({
      Name: document.getElementById("nc-name").value,
      Industry: document.getElementById("nc-industry").value,
      ContactName: document.getElementById("nc-contact-name").value,
      ContactEmail: document.getElementById("nc-contact-email").value,
      ContactPhone: document.getElementById("nc-contact-phone").value,
      __RequestVerificationToken: window.smartjob.antiForgeryToken
    });
    const response = await fetch(window.smartjob.createCustomerUrl, { method: "POST", body });
    const result = await response.json();
    if (!result.ok) {
      ncError.textContent = result.error || "Couldn't create that customer.";
      return;
    }
    const opt = document.createElement("option");
    opt.value = result.id;
    opt.textContent = result.name;
    opt.selected = true;
    customerSelect.appendChild(opt);
    newCustomerPanel.hidden = true;
    await loadSitesForCustomer(result.id, null);
  });

  const newSitePanel = document.getElementById("new-site-panel");
  const nsSaveBtn = document.getElementById("ns-save-btn");
  const nsCancelBtn = document.getElementById("ns-cancel-btn");
  const nsError = document.getElementById("ns-error");

  newSiteBtn?.addEventListener("click", () => {
    if (!customerSelect.value) {
      alert("Choose a customer first.");
      return;
    }
    newSitePanel.hidden = !newSitePanel.hidden;
  });
  nsCancelBtn?.addEventListener("click", () => {
    newSitePanel.hidden = true;
    nsError.textContent = "";
  });
  nsSaveBtn?.addEventListener("click", async () => {
    nsError.textContent = "";
    const body = new URLSearchParams({
      CustomerId: customerSelect.value,
      Name: document.getElementById("ns-name").value,
      Address: document.getElementById("ns-address").value,
      SiteType: document.getElementById("ns-site-type").value,
      __RequestVerificationToken: window.smartjob.antiForgeryToken
    });
    const response = await fetch(window.smartjob.createSiteUrl, { method: "POST", body });
    const result = await response.json();
    if (!result.ok) {
      nsError.textContent = result.error || "Couldn't create that site.";
      return;
    }
    const opt = document.createElement("option");
    opt.value = result.id;
    opt.textContent = result.name;
    opt.selected = true;
    siteSelect.appendChild(opt);
    newSitePanel.hidden = true;
  });
})();