/**
 * TgaBuilder Homepage — Main Script
 * Handles theme toggling and smooth scroll behavior.
 */

(function () {
  'use strict';

  // --- Theme Toggle ---
  const THEME_KEY = 'tgabuilder-theme';
  const html = document.documentElement;
  const toggle = document.getElementById('theme-toggle');

  function getStoredTheme() {
    return localStorage.getItem(THEME_KEY);
  }

  function getSystemTheme() {
    return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
  }

  function applyTheme(theme) {
    html.setAttribute('data-theme', theme);
  }

  // Initialize theme
  const stored = getStoredTheme();
  applyTheme(stored || getSystemTheme());

  // Toggle on click
  if (toggle) {
    toggle.addEventListener('click', function () {
      const current = html.getAttribute('data-theme');
      const next = current === 'dark' ? 'light' : 'dark';
      applyTheme(next);
      localStorage.setItem(THEME_KEY, next);
    });
  }

  // Listen for system preference changes (if no stored preference)
  window.matchMedia('(prefers-color-scheme: light)').addEventListener('change', function (e) {
    if (!getStoredTheme()) {
      applyTheme(e.matches ? 'light' : 'dark');
    }
  });

  // --- Navbar scroll shadow ---
  const navbar = document.querySelector('.navbar');
  if (navbar) {
    window.addEventListener('scroll', function () {
      if (window.scrollY > 10) {
        navbar.style.boxShadow = '0 2px 12px rgba(0, 0, 0, 0.15)';
      } else {
        navbar.style.boxShadow = 'none';
      }
    }, { passive: true });
  }

  // --- Lazy video play on intersect (for performance) ---
  const videos = document.querySelectorAll('.demo-video video');
  if ('IntersectionObserver' in window && videos.length > 0) {
    const observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.play();
        } else {
          entry.target.pause();
        }
      });
    }, { threshold: 0.25 });

    videos.forEach(function (video) {
      video.pause();
      observer.observe(video);
    });
  }
})();
