const form = document.getElementById('contactForm');
const sendBtn = document.getElementById('sendBtn');
const status = document.getElementById('status');

function clearErrors() {
    ['err-name', 'err-email', 'err-subject', 'err-message'].forEach(id => {
        document.getElementById(id).textContent = '';
    });
    status.textContent = '';
}

function validate() {
    clearErrors();
    let ok = true;
    const name = form.name.value.trim();
    const email = form.email.value.trim();
    const subject = form.subject.value.trim();
    const message = form.message.value.trim();

    if (name.length < 2) { document.getElementById('err-name').textContent = 'الرجاء إدخال اسم صحيح.'; ok = false; }
    const emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRe.test(email)) { document.getElementById('err-email').textContent = 'الرجاء إدخال بريد إلكتروني صالح.'; ok = false; }
    if (subject.length < 2) { document.getElementById('err-subject').textContent = 'أدخل موضوعاً.'; ok = false; }
    if (message.length < 6) { document.getElementById('err-message').textContent = 'الرسالة قصيرة جداً.'; ok = false; }

    return ok;
}

form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (!validate()) return;

    sendBtn.disabled = true;
    sendBtn.textContent = 'يتم الإرسال...';
    status.textContent = '';

    // === طريقة افتراضية: ترسل بيانات JSON إلى نقطة النهاية /api/contact ===
    // قم بتعديل URL في fetch إلى المسار الذي يعالج الإيميل/التخزين في السيرفر لديك.
    try {
        const resp = await fetch('/home/SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                name: form.name.value.trim(),
                email: form.email.value.trim(),
                subject: form.subject.value.trim(),
                message: form.message.value.trim()
            })
        });

        if (resp.ok) {
            status.textContent = 'تم إرسال رسالتك بنجاح — شكراً لتواصلك معنا.';
            form.reset();
        } else {
            const txt = await resp.text().catch(() => null);
            status.textContent = 'حدث خطأ أثناء الإرسال. ' + (txt || '');
        }
    } catch (err) {
        status.textContent = 'تعذر الاتصال بالخادم. تحقق من اتصالك أو حاول لاحقاً.';
    } finally {
        sendBtn.disabled = false;
        sendBtn.textContent = 'أرسل الرسالة';
    }
});