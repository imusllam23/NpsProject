
// نسخ الرابط
function copyLink(btn) {
    const url = window.location.href;
    navigator.clipboard.writeText(url).then(function () {

    }).catch(function (err) {
        alert('فشل النسخ: ' + err);
    }).finally(() => {
        btn.blur();
    });
}

// Scroll Animation
document.addEventListener('DOMContentLoaded', function () {
    const articleContent = document.querySelector('.article-content');
    if (articleContent) {
        articleContent.style.opacity = '0';
        articleContent.style.transform = 'translateY(20px)';
        articleContent.style.transition = 'all 0.6s ease';

        setTimeout(() => {
            articleContent.style.opacity = '1';
            articleContent.style.transform = 'translateY(0)';
        }, 100);
    }
});