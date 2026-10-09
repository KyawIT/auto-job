// jsdom has no <dialog> behaviour; give it just enough to open and close.
const dialog = globalThis.HTMLDialogElement?.prototype;
if (dialog && !dialog.showModal) {
  dialog.showModal = function (this: HTMLDialogElement) {
    this.setAttribute('open', '');
  };
  dialog.show = dialog.showModal;
  dialog.close = function (this: HTMLDialogElement) {
    if (!this.hasAttribute('open')) return;
    this.removeAttribute('open');
    this.dispatchEvent(new Event('close'));
  };
}
