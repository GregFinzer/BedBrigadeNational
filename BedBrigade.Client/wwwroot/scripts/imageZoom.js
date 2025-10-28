(function(){
 function ensureStyle(){
 if(document.getElementById('bb-image-zoom-style')) return;
 const style = document.createElement('style');
 style.id = 'bb-image-zoom-style';
 style.textContent = `
/* Enable hover magnifier on images inside the enabled container */
.bb-image-zoom-enabled img { cursor: zoom-in; }

/* Overlay fade/zoom transitions */
.bb-image-zoom-overlay {
 position: fixed; inset:0; background: rgba(0,0,0,0.92);
 display: flex; align-items: center; justify-content: center;
 z-index:2147483647; cursor: zoom-out; opacity:0;
 transition: opacity 220ms ease;
}
.bb-image-zoom-overlay.open { opacity:1; }
.bb-image-zoom-overlay img {
 max-width:98%; max-height:98%; object-fit: contain;
 box-shadow: 0 10px 40px rgba(0,0,0,0.6); border-radius:4px;
 transform: scale(0.95); transform-origin: center center;
 transition: transform 220ms ease;
 will-change: transform;
 cursor: zoom-out; /* Ensure cursor shows zoom-out when zoomed */
}
.bb-image-zoom-overlay.open img { transform: scale(1); }

@media (prefers-reduced-motion: reduce) {
 .bb-image-zoom-overlay { transition: none; }
 .bb-image-zoom-overlay img { transition: none; }
}
`;
 document.head.appendChild(style);
 }

 function createOverlay(src, srcset, alt){
 const overlay = document.createElement('div');
 overlay.className = 'bb-image-zoom-overlay';
 overlay.tabIndex =0;

 const img = document.createElement('img');
 img.src = src || '';
 if(srcset) img.srcset = srcset;
 if(alt) img.alt = alt;

 overlay.appendChild(img);

 // clicking the overlay (anywhere) closes it (zoom out)
 overlay.addEventListener('click', function(e){
 e.stopPropagation();
 removeOverlay();
 });

 // clicking the image also closes it (zoom out)
 img.addEventListener('click', function(e){
 e.stopPropagation();
 removeOverlay();
 });

 return overlay;
 }

 let currentOverlay = null;
 let isClosing = false;

 function removeOverlay(){
 if(!currentOverlay || isClosing) return;
 isClosing = true;
 const overlay = currentOverlay;
 // start close animation (fade/zoom out)
 overlay.classList.remove('open');

 const cleanup = () => {
 if(!currentOverlay) return; // already cleaned
 overlay.remove();
 currentOverlay = null;
 isClosing = false;
 document.body.style.overflow = '';
 };

 // Only remove after the overlay's opacity transition finishes
 const handleTransitionEnd = (ev) => {
 if(ev.target !== overlay) return; // ignore img transition
 overlay.removeEventListener('transitionend', handleTransitionEnd);
 cleanup();
 };

 // Fallback in case transitionend doesn't fire
 const fallback = setTimeout(() => {
 overlay.removeEventListener('transitionend', handleTransitionEnd);
 cleanup();
 },300);

 overlay.addEventListener('transitionend', function(ev){
 clearTimeout(fallback);
 handleTransitionEnd(ev);
 });
 }

 function onContainerClick(e){
 // If overlay already open, close it
 if(currentOverlay){
 removeOverlay();
 return;
 }

 const target = e.target;
 if(!target || target.tagName !== 'IMG') return;

 // figure out the best source (handle responsive or lazy images)
 const src = target.currentSrc || target.getAttribute('src') || target.dataset.src || '';
 const srcset = target.getAttribute('srcset') || '';
 const alt = target.getAttribute('alt') || '';

 ensureStyle();

 currentOverlay = createOverlay(src, srcset, alt);
 document.body.appendChild(currentOverlay);
 // prevent background scroll while zoomed
 document.body.style.overflow = 'hidden';
 currentOverlay.focus();

 // next frame: trigger opening transition (zoom/fade in)
 requestAnimationFrame(() => {
 currentOverlay.classList.add('open');
 });
 }

 function init(){
 const container = document.getElementById('zoom-body') || document.body;
 // Add class to enable hover cursor only within the container
 container.classList.add('bb-image-zoom-enabled');
 // Use event delegation so dynamically injected images are handled
 container.addEventListener('click', onContainerClick);

 // Close overlay with Escape
 document.addEventListener('keydown', function(e){
 if(e.key === 'Escape' && currentOverlay) removeOverlay();
 });

 // Observe DOM mutations in container to ensure newly added images are still handled (delegation covers this but keep for robustness)
 if(window.MutationObserver && container){
 const mo = new MutationObserver(() => {});
 mo.observe(container, { childList: true, subtree: true });
 }
 }

 if(document.readyState === 'loading'){
 document.addEventListener('DOMContentLoaded', init);
 } else {
 init();
 }
})();
