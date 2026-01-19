/* FACILITY.IA - Core Script 
   Versão Corrigida: Menu Mobile Único + Loading State + Segurança
*/

document.addEventListener('DOMContentLoaded', function() {
    // 1. Inicializa Partículas (Se existir na página)
    initParticles();
    
    // 2. Verifica Login (Segurança básica de front)
    checkAuthRedirect();
    
    // 3. Ativa Animações de Entrada
    runPageAnimations();

    // 4. Ativa Lógica Mobile (Se for tela pequena)
    if (window.innerWidth <= 991) {
        initMobileNav();
        fixMobileVh();
    }
});

/* --- SISTEMA DE ANIMAÇÃO FLUIDA --- */
function runPageAnimations() {
    const elementsToAnimate = document.querySelectorAll('.app-card, .hero-content, .login-card, .pro-card, .table-row');
    
    elementsToAnimate.forEach((el, index) => {
        el.style.opacity = '0';
        el.classList.add('animate-slide-up');
        el.style.animationDelay = `${index * 0.08}s`;
    });

    const inputs = document.querySelectorAll('input, textarea');
    inputs.forEach(input => {
        input.addEventListener('focus', () => input.parentElement.classList.add('focused'));
        input.addEventListener('blur', () => input.parentElement.classList.remove('focused'));
    });
}

/* --- NOVO: LOADING STATE GLOBAL --- 
   Use: setLoading(true) para bloquear a tela enquanto carrega
*/
window.setLoading = function(isLoading, text = "Processando...") {
    let loader = document.getElementById('global-loader');
    
    if (isLoading) {
        if (!loader) {
            loader = document.createElement('div');
            loader.id = 'global-loader';
            loader.innerHTML = `<div class="spinner"></div><p>${text}</p>`;
            document.body.appendChild(loader);
        }
        loader.classList.add('active');
        document.body.style.cursor = 'wait';
    } else {
        if (loader) {
            loader.classList.remove('active');
            setTimeout(() => loader.remove(), 300);
        }
        document.body.style.cursor = 'default';
    }
}

/* --- SISTEMA DE NOTIFICAÇÕES (TOAST) --- */
window.showToast = function(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    
    let icon = type === 'success' ? '<i class="fas fa-check-circle" style="color:#00ff88"></i>' : 
               type === 'error' ? '<i class="fas fa-times-circle" style="color:#ff5555"></i>' : 
               '<i class="fas fa-info-circle" style="color:#00ccff"></i>';

    toast.innerHTML = `${icon} <span style="font-weight:500">${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        setTimeout(() => toast.remove(), 400);
    }, 4000);
}

/* --- PARTICULAS --- */
function initParticles() {
    const particlesDiv = document.getElementById('particles-js');
    if (particlesDiv && typeof particlesJS !== 'undefined') {
        particlesJS("particles-js", {
            "particles": {
                "number": { "value": 30, "density": { "enable": true, "value_area": 800 } },
                "color": { "value": "#00ff88" },
                "shape": { "type": "circle" },
                "opacity": { "value": 0.2, "random": true },
                "size": { "value": 3, "random": true },
                "line_linked": { "enable": true, "distance": 150, "color": "#00ff88", "opacity": 0.1, "width": 1 },
                "move": { "enable": true, "speed": 1 }
            },
            "interactivity": {
                "detect_on": "canvas",
                "events": { "onhover": { "enable": true, "mode": "grab" }, "onclick": { "enable": true, "mode": "push" } },
                "modes": { "grab": { "distance": 140, "line_linked": { "opacity": 0.4 } } }
            },
            "retina_detect": true
        });
    }
}

/* --- UTILITÁRIOS DE SEGURANÇA --- */
function checkAuthRedirect() {
    const token = localStorage.getItem('token');
    const path = window.location.pathname.toLowerCase();
    
    // Lista de páginas que NÃO precisam de login
    const publicPages = ['login.html', 'index.html', 'cadastrodeusuario.html', 'cadastrodeagente.html', '/'];
    
    // Se não for pública e não tiver token, manda pro login
    const isPublic = publicPages.some(p => path.endsWith(p) || path === '/');

    if (!token && !isPublic) {
        // window.location.href = 'login.html'; // Descomente para ativar proteção
    }
}

window.fazerLogout = function() {
    localStorage.removeItem('token');
    localStorage.removeItem('userData');
    showToast("Saindo...", "info");
    setTimeout(() => window.location.href = 'login.html', 1000);
}

/* --- MOBILE NAV & UI FIXES --- */
function fixMobileVh() {
    let vh = window.innerHeight * 0.01;
    document.documentElement.style.setProperty('--vh', `${vh}px`);
    window.addEventListener('resize', () => {
        let vh = window.innerHeight * 0.01;
        document.documentElement.style.setProperty('--vh', `${vh}px`);
    });
}

function initMobileNav() {
    // Remove botões antigos para evitar duplicidade
    const oldBtns = document.querySelectorAll('#mobile-nav-trigger, .mobile-backdrop');
    oldBtns.forEach(b => b.remove());

    const sidebar = document.querySelector('.sidebar') || document.getElementById('sidebar');
    if (!sidebar) return;

    // Botão Flutuante
    const btn = document.createElement('div');
    btn.id = 'mobile-nav-trigger';
    btn.innerHTML = '<i class="fas fa-bars"></i>';
    document.body.appendChild(btn);

    // Fundo Escuro
    const backdrop = document.createElement('div');
    backdrop.className = 'mobile-backdrop';
    document.body.appendChild(backdrop);

    function toggleMenu() {
        const isOpen = sidebar.classList.toggle('app-open');
        backdrop.classList.toggle('active');
        btn.innerHTML = isOpen ? '<i class="fas fa-times"></i>' : '<i class="fas fa-bars"></i>';
        document.body.style.overflow = isOpen ? 'hidden' : '';
    }

    btn.addEventListener('click', (e) => { e.stopPropagation(); toggleMenu(); });
    backdrop.addEventListener('click', toggleMenu);

    sidebar.querySelectorAll('a').forEach(link => {
        link.addEventListener('click', () => {
            if (sidebar.classList.contains('app-open')) toggleMenu();
        });
    });
}