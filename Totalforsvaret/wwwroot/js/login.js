document.querySelectorAll('.show-password').forEach(toggle => {
    const password = document.getElementById(toggle.dataset.target || 'password');
    toggle.addEventListener('click', () => {
        const visible = password.type === 'password';
        password.type = visible ? 'text' : 'password';
        toggle.setAttribute('aria-pressed', String(visible));
        toggle.setAttribute('aria-label', visible ? 'Skjul passord' : 'Vis passord');

    });
});
const message = document.querySelector('#form-message');
function showMessage(text) { message.textContent = text; message.hidden = false; }

const registration = document.querySelector('#register-form');
const confirmation = document.querySelector('#confirm-password');
function validateConfirmation() {
    confirmation?.setCustomValidity(confirmation.value === document.querySelector('#password').value ? '' : 'Passordene må være like.');
}
registration?.addEventListener('input', validateConfirmation);
registration?.addEventListener('submit', event => {
    validateConfirmation();
    if (!registration.reportValidity()) event.preventDefault();
});
document.querySelectorAll('[data-info]').forEach(button => button.addEventListener('click', () => showMessage(button.dataset.info)));
function updateActor() {
    const selected = document.querySelector('input[name="actor"]:checked');
    if (!selected) return;
    const civil = selected.value === 'civil';
    const registerLink = document.querySelector('.account-links .register-button');
    if (registerLink) registerLink.hidden = !civil;
    const email = document.querySelector('#email');
    if (email) email.placeholder = civil ? 'Skriv inn mail' : 'Skriv inn jobb-mail';
}
document.querySelectorAll('input[name="actor"]').forEach(input => input.addEventListener('change', updateActor));
updateActor();
document.querySelector('#forgot-password-form')?.addEventListener('submit', event => event.preventDefault());
