window.initCustomDrag = (containerId, dotnetRef) => {
    const container = document.getElementById(containerId);
    if (!container) return;

    let draggedRow = null;
    let ghostElement = null;
    let startIndex = -1;
    let currentTargetIndex = -1;

    const onPointerDown = (e) => {
        const handle = e.target.closest('.drag-handle');
        if (!handle) return;

        e.preventDefault(); // Stop mobile scrolling

        draggedRow = handle.closest('.item-row');
        if (!draggedRow) return;

        startIndex = Array.from(container.children).indexOf(draggedRow);
        currentTargetIndex = startIndex;

        // Create floating ghost row
        ghostElement = draggedRow.cloneNode(true);
        const rect = draggedRow.getBoundingClientRect();
        ghostElement.style.position = 'fixed';
        ghostElement.style.top = rect.top + 'px';
        ghostElement.style.left = rect.left + 'px';
        ghostElement.style.width = rect.width + 'px';
        ghostElement.style.opacity = '0.9';
        ghostElement.style.pointerEvents = 'none'; // Crucial: lets pointer events pass through to find target row
        ghostElement.style.zIndex = '9999';
        ghostElement.style.boxShadow = '0 5px 15px rgba(0,0,0,0.25)';

        document.body.appendChild(ghostElement);
        draggedRow.style.opacity = '0.3';

        handle.setPointerCapture(e.pointerId);
    };

    const onPointerMove = (e) => {
        if (!ghostElement) return;
        e.preventDefault();

        // Move ghost
        ghostElement.style.top = (e.clientY - 20) + 'px';

        // Find target row underneath
        const target = document.elementFromPoint(e.clientX, e.clientY);
        if (!target) return;

        const targetRow = target.closest('.item-row');

        // Clear old highlights
        Array.from(container.children).forEach(child => {
            child.style.borderTop = '';
            child.style.borderBottom = '';
        });

        // Draw drop indicator line
        if (targetRow && targetRow.parentElement === container && targetRow !== draggedRow) {
            currentTargetIndex = Array.from(container.children).indexOf(targetRow);
            if (currentTargetIndex < startIndex) {
                targetRow.style.borderTop = '3px solid #007bff';
            } else {
                targetRow.style.borderBottom = '3px solid #007bff';
            }
        } else {
            currentTargetIndex = startIndex;
        }
    };

    const onPointerUp = (e) => {
        if (!draggedRow) return;

        const handle = e.target.closest('.drag-handle');
        if (handle) handle.releasePointerCapture(e.pointerId);

        // Cleanup visual changes
        if (ghostElement) ghostElement.remove();
        ghostElement = null;

        draggedRow.style.opacity = '1';
        Array.from(container.children).forEach(child => {
            child.style.borderTop = '';
            child.style.borderBottom = '';
        });

        // Send result back to Blazor
        if (currentTargetIndex !== -1 && currentTargetIndex !== startIndex) {
            dotnetRef.invokeMethodAsync('MoveIngredient', startIndex, currentTargetIndex);
        }

        draggedRow = null;
        startIndex = -1;
        currentTargetIndex = -1;
    };

    container.addEventListener('pointerdown', onPointerDown);
    container.addEventListener('pointermove', onPointerMove);
    container.addEventListener('pointerup', onPointerUp);
    container.addEventListener('pointercancel', onPointerUp);

    // Redundancy for mobile
    container.addEventListener('touchstart', (e) => {
        if (e.target.closest('.drag-handle')) e.preventDefault();
    }, { passive: false });
};