window.MacroPrepDB = {
    db: null,
    dbName: "MacroPrepOffline",
    version: 4,

    init: function () {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.version);

            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                const txn = event.target.transaction; // Get active transaction

                let listsStore;
                if (!db.objectStoreNames.contains('lists')) {
                    listsStore = db.createObjectStore('lists', { keyPath: 'id' });
                } else {
                    listsStore = txn.objectStore('lists');
                }

                if (!listsStore.indexNames.contains('ownerId')) {
                    listsStore.createIndex('ownerId', 'ownerId', { unique: false });
                }

                let itemsStore;
                if (!db.objectStoreNames.contains('items')) {
                    itemsStore = db.createObjectStore('items', { keyPath: 'id' });
                } else {
                    itemsStore = txn.objectStore('items');
                }

                if (!itemsStore.indexNames.contains('listId')) {
                    itemsStore.createIndex('listId', 'listId', { unique: false });
                }
            };

            request.onsuccess = (event) => {
                this.db = event.target.result;
                console.log("✅ MacroPrep DB Connected (v3)");
                resolve("Connected");
            };

            request.onerror = (event) => {
                reject(event.target.error);
            };
        });
    },

    save: function (storeName, item) {
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction(storeName, 'readwrite');
            const store = tx.objectStore(storeName);
            store.put(item);
            tx.oncomplete = () => resolve("Saved");
            tx.onerror = () => reject(tx.error);
        });
    },

    getAll: function (storeName) {
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction(storeName, 'readonly');
            const store = tx.objectStore(storeName);
            const request = store.getAll();
            request.onsuccess = () => resolve(request.result);
        });
    },

    getItemsByList: function (listId) {
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction('items', 'readonly');
            const store = tx.objectStore('items');

            const index = store.index('listId');
            const request = index.getAll(listId);

            request.onsuccess = () => resolve(request.result);
        });
    },

    delete: function (storeName, id) {
        return new Promise((resolve, reject) => {
            const tx = this.db.transaction(storeName, 'readwrite');
            const store = tx.objectStore(storeName);
            store.delete(id);
            tx.oncomplete = () => resolve("Deleted");
        });
    }
};