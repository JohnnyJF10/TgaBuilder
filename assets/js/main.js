/**
 * TrLynx Homepage — Main Script
 * Handles theme toggling, smooth scroll, and video carousel.
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

  // --- Video Carousel ---
  var video = document.querySelector('.carousel-video');
  var thumbs = document.querySelectorAll('.carousel-thumb');
  var captionTitle = document.querySelector('.carousel-caption-title');
  var captionText = document.querySelector('.carousel-caption-text');
  var currentIndex = 0;

  function activateSlide(index) {
    // Reset all thumbs
    thumbs.forEach(function (t) {
      t.classList.remove('active');
      t.querySelector('.thumb-progress').style.width = '0%';
      t.querySelector('.thumb-progress').style.transitionDuration = '0s';
    });

    currentIndex = index;
    var thumb = thumbs[index];
    thumb.classList.add('active');

    // Load and play video
    var src = thumb.getAttribute('data-src');
    captionTitle.textContent = thumb.getAttribute('data-title');
    captionText.textContent = thumb.getAttribute('data-desc');

    video.src = src;
    video.load();
    video.play().catch(function () { /* autoplay blocked */ });
  }

  // Update progress bar as video plays
  function onTimeUpdate() {
    if (!video.duration) return;
    var progress = (video.currentTime / video.duration) * 100;
    var activeThumb = thumbs[currentIndex];
    if (activeThumb) {
      var bar = activeThumb.querySelector('.thumb-progress');
      bar.style.transitionDuration = '0.2s';
      bar.style.width = progress + '%';
    }
  }

  // Advance to next video when current one ends
  function onVideoEnded() {
    var nextIndex = (currentIndex + 1) % thumbs.length;
    activateSlide(nextIndex);
  }

  if (video && thumbs.length > 0) {
    video.addEventListener('timeupdate', onTimeUpdate);
    video.addEventListener('ended', onVideoEnded);

    // Thumb click handlers
    thumbs.forEach(function (thumb, idx) {
      thumb.addEventListener('click', function () {
        activateSlide(idx);
      });
    });

    // Start first video only when carousel is visible
    if ('IntersectionObserver' in window) {
      var carouselObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            activateSlide(0);
            carouselObserver.disconnect();
          }
        });
      }, { threshold: 0.2 });
      carouselObserver.observe(document.querySelector('.carousel-stage'));
    } else {
      activateSlide(0);
    }
  }
})();
