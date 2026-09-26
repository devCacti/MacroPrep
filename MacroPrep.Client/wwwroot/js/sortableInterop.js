window.sortableInterop = {

    initialize: function (element, dotNetHelper, callbackName) {
        console.log("element =", element);
        console.log("HTMLElement?", element instanceof HTMLElement);
        console.log("constructor =", element?.constructor?.name);
        console.log("callbackname =", callbackName);

        // Prevent creating multiple Sortable instances
        if (element.sortableInstance)
            return;

        element.sortableInstance = Sortable.create(element, {

            animation: 150,

            handle: ".drag-handle",

            ghostClass: "sortable-ghost",

            chosenClass: "sortable-chosen",

            dragClass: "sortable-drag",

            onEnd: async function (evt) {

                console.log("Old:", evt.oldIndex);
                console.log("New:", evt.newIndex);
                console.log("Callback:", callbackName);
                console.log("DotNet:", dotNetHelper);

                if (evt.oldIndex === evt.newIndex)
                    return;

                try {

                    await dotNetHelper.invokeMethodAsync(
                        callbackName,
                        evt.oldIndex,
                        evt.newIndex);

                    console.log("Invoke succeeded");

                } catch (e) {

                    console.error("Invoke failed", e);
                }
            }
        });
    }
};