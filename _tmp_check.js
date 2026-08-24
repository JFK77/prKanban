
        import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.0/firebase-app.js";
        import { getFirestore, enableIndexedDbPersistence, collection, getDocs, addDoc, updateDoc, doc, deleteDoc } from "https://www.gstatic.com/firebasejs/10.12.0/firebase-firestore.js";

        // Your web app's Firebase configuration
        const firebaseConfig = {
            apiKey: "AIzaSyDE2TQ0t5TlhiBYnsdYOZbXM3iuBTAU2OM", // This is a placeholder, ensure it's your actual key
            authDomain: "prkanban.firebaseapp.com",
            projectId: "prkanban",
            storageBucket: "prkanban.firebasestorage.app",
            messagingSenderId: "345953337349",
            appId: "1:345953337349:web:6a998e063e171d9bf19c6c",
            measurementId: "G-YKRZB7MX7H"
        };

        // Initialize Firebase
        const app = initializeApp(firebaseConfig);
        const db = getFirestore(app);

        // --- LOCAL STORAGE HELPERS ---
        function loadFromStorage(key, defaultValue) {
            const data = localStorage.getItem(key);
            if (data === null) return defaultValue;
            try {
                return JSON.parse(data);
            } catch (error) {
                console.warn(`Could not parse localStorage key "${key}". Resetting it.`);
                return defaultValue;
            }
        }

        function saveToStorage(key, value) {
            try {
                localStorage.setItem(key, JSON.stringify(value));
            } catch (error) {
                console.warn(`Could not save "${key}" to localStorage:`, error);
            }
        }

        function persistTaskSnapshot() {
            saveToStorage('kanban_tasks', tasks);
        }

        function normalizeTaskPayload(taskData, isUpdate = false) {
            const now = new Date().toISOString();
            const base = { ...taskData };
            if (!isUpdate) {
                base.createdAt = base.createdAt || now;
            }
            base.updatedAt = now;
            return base;
        }

        async function setupFirestorePersistence() {
            try {
                await enableIndexedDbPersistence(db);
                console.log("Firestore offline persistence enabled.");
            } catch (error) {
                if (error.code === 'failed-precondition') {
                    console.warn('Firestore persistence can only be enabled in one tab at a time.');
                } else if (error.code === 'unimplemented') {
                    console.warn('This browser does not support Firestore offline persistence.');
                } else {
                    console.warn('Firestore persistence could not be enabled:', error);
                }
            }
        }

        // --- FIRESTORE CRUD FUNCTIONS ---
        async function loadTasksFromCloud() {
            try {
                const querySnapshot = await getDocs(collection(db, "tasks"));
                const cloudTasks = [];
                querySnapshot.forEach((docSnapshot) => {
                    cloudTasks.push({ ...docSnapshot.data(), id: docSnapshot.id });
                });

                updateDynamicListsFromTasks(cloudTasks);
                console.log("Tasks successfully loaded from Firestore:", cloudTasks);
                tasks = cloudTasks;
                persistTaskSnapshot();

                if (tasks.length === 0) {
                    console.log("No tasks found in Firestore. The board will be empty.");
                }

                updateAllDropdowns();
                renderBoard();
            } catch (error) {
                console.error("Error loading tasks from Firestore:", error);
                const localFallback = loadFromStorage('kanban_tasks', []);
                tasks = Array.isArray(localFallback) ? localFallback : [];
                showToast("Firestore erreichbar? Lokale letzte Sicherung wird geladen.");
                updateAllDropdowns();
                renderBoard();
            }
        }

        async function addTaskToCloud(taskData) {
            try {
                const payload = normalizeTaskPayload(taskData, false);
                const docRef = await addDoc(collection(db, "tasks"), payload);
                const newTask = { ...payload, id: docRef.id };
                tasks = [...tasks, newTask];
                persistTaskSnapshot();
                console.log("Task added with ID: ", docRef.id);
                return docRef.id;
            } catch (e) {
                console.error("Error adding document: ", e);
                showToast("Task konnte nicht in Firebase gespeichert werden.");
                return null;
            }
        }

        async function updateTaskInCloud(taskId, taskData) {
            try {
                const taskRef = doc(db, "tasks", taskId);
                const payload = normalizeTaskPayload(taskData, true);
                await updateDoc(taskRef, payload);

                const taskIndex = tasks.findIndex((task) => task.id === taskId);
                if (taskIndex !== -1) {
                    tasks[taskIndex] = { ...tasks[taskIndex], ...payload };
                }
                persistTaskSnapshot();
                console.log(`Task ${taskId} updated in Firestore.`);
                return true;
            } catch (e) {
                console.error("Error updating document: ", e);
                showToast(`Task ${taskId.slice(0, 8)}... konnte nicht gespeichert werden.`);
                return false;
            }
        }

        async function deleteTaskFromCloud(taskId) {
            try {
                await deleteDoc(doc(db, "tasks", taskId));
                tasks = tasks.filter((task) => task.id !== taskId);
                persistTaskSnapshot();
                console.log(`Task ${taskId} deleted from Firestore.`);
                return true;
            } catch (e) {
                console.error("Error deleting document: ", e);
                showToast(`Task ${taskId.slice(0, 8)}... konnte nicht gelÃ¶scht werden.`);
                return false;
            }
        }

        // --- GLOBAL STATE & CONFIGURATION ---
        let tasks = [];
        let selectedModalTags = [];
        let draggedTaskId = null;
        let selectedTaskId = null;
        const collapsedSwimlanes = new Set();

        let owners = [];
        let customers = [];
        let tagsConfig = loadFromStorage('kanban_tagsConfig', {
            "Prio A": "#ef4444", "Database": "#3b82f6", "Konzept": "#a855f7",
            "Hardware": "#f97316", "Network": "#06b6d4", "Freigabe": "#10b981",
            "Review": "#eab308", "Doku": "#64748b"
        });

        let workflowPaths = loadFromStorage('kanban_workflowPaths', {
            "Installation": { path: ["ToDo", "InProgress", "Done"], color: "#10b981" }, // Green
            "Typicals creation": { path: ["ToDo", "Analysis", "InProgress", "Done"], color: "#f97316" } // Orange
        });

        // --- Data Migration for backward compatibility ---
        // This ensures that if a user has old data in localStorage, it gets updated to the new object format.
        let needsSave = false;
        for (const type in workflowPaths) {
            if (Array.isArray(workflowPaths[type])) {
                console.log(`Migrating old task type format for: ${type}`);
                const oldPath = workflowPaths[type];
                workflowPaths[type] = {
                    path: oldPath,
                    color: type === 'Installation' ? '#10b981' : (type === 'Typicals creation' ? '#f97316' : '#64748b') // Default colors
                };
                needsSave = true;
            }
        }
        if (needsSave) {
            saveToStorage('kanban_workflowPaths', workflowPaths);
        }
        // --- End of Migration ---
        
        const wipLimits = loadFromStorage('kanban_wipLimits', {
            "ToDo": 5, "Analysis": 4, "InProgress": 3, "Done": null
        });

        function saveAllState() {
            saveToStorage('kanban_tagsConfig', tagsConfig);
            saveToStorage('kanban_workflowPaths', workflowPaths);
            saveToStorage('kanban_wipLimits', wipLimits);
        }

        // --- UI ELEMENT SELECTORS ---
        const filterOwner = document.getElementById('filter-owner');
        const filterCustomer = document.getElementById('filter-customer');
        const filterType = document.getElementById('filter-type');
        const groupPrimary = document.getElementById('group-primary');
        const groupSecondary = document.getElementById('group-secondary');

        // --- UI HELPER FUNCTIONS ---
        function showToast(message) {
            const toast = document.getElementById('toast');
            document.getElementById('toast-message').innerText = message;
            toast.className = "show";
            setTimeout(() => { toast.className = toast.className.replace("show", ""); }, 3500);
        }

        function getColLabel(col) {
            const labels = { "ToDo": "To Do", "Analysis": "Analysis", "InProgress": "In Progress", "Done": "Done" };
            return labels[col] || col;
        }

        // --- RENDER FUNCTIONS ---
        function renderBoard() {
            const container = document.getElementById('board-container');
            if (!container) return;
            container.innerHTML = "";

            const activeTasks = getFilteredTasks();
            const pGroup = groupPrimary.value;
            const sGroup = groupSecondary.value;

            if (pGroup === 'NONE') {
                container.appendChild(createBoardElement("Gesamtes Board", activeTasks));
            } else {
                const group1Keys = [...new Set(tasks.map(t => t[pGroup]))].sort();
                
                group1Keys.forEach(g1Value => {
                    const g1Tasks = activeTasks.filter(t => t[pGroup] === g1Value);
                    const swimlaneId = `p-${pGroup}-${g1Value}`.replace(/\s+/g, '-');
                    const isCollapsed = collapsedSwimlanes.has(swimlaneId);

                    const swimlaneSection = document.createElement('div');
                    swimlaneSection.className = "mb-6 bg-slate-900/40 border border-slate-800 rounded-xl p-5 shadow-sm transition-all duration-300";
                    
                    swimlaneSection.innerHTML = `
                        <div onclick="toggleSwimlane('${swimlaneId}')" class="flex items-center justify-between cursor-pointer select-none pb-2 border-b border-slate-800/80 mb-4 group/header">
                            <div class="flex items-center gap-3">
                                <span class="text-slate-400 group-hover/header:text-white transition-colors duration-200">
                                    <i class="fa-solid ${isCollapsed ? 'fa-chevron-right' : 'fa-chevron-down'} text-xs"></i>
                                </span>
                                <span class="text-xs uppercase bg-blue-500/10 text-blue-400 font-bold px-2.5 py-1 rounded-md border border-blue-500/20">
                                    ${pGroup.toUpperCase()}: ${g1Value}
                                </span>
                                <span class="text-xs text-slate-500">(${g1Tasks.length} Tasks)</span>
                            </div>
                        </div>
                    `;

                    const contentContainer = document.createElement('div');
                    if (isCollapsed) contentContainer.className = "hidden";

                    if (sGroup !== 'NONE' && sGroup !== pGroup) {
                        const group2Keys = [...new Set(tasks.map(t => t[sGroup]))].sort();
                        group2Keys.forEach(g2Value => {
                            const g2Tasks = g1Tasks.filter(t => t[sGroup] === g2Value);
                            const subSwimlaneId = `s-${swimlaneId}-${sGroup}-${g2Value}`.replace(/\s+/g, '-');
                            const isSubCollapsed = collapsedSwimlanes.has(subSwimlaneId);

                            if (g2Tasks.length > 0) {
                                const subSwimlane = document.createElement('div');
                                subSwimlane.className = "ml-4 mb-4";
                                
                                subSwimlane.innerHTML = `
                                    <div onclick="toggleSwimlane('${subSwimlaneId}')" class="flex items-center justify-between cursor-pointer select-none mb-2 py-1 group/subheader">
                                        <div class="flex items-center gap-2">
                                            <span class="text-slate-500 group-hover/subheader:text-slate-300 transition-colors duration-200">
                                                <i class="fa-solid ${isSubCollapsed ? 'fa-chevron-right' : 'fa-chevron-down'} text-[10px]"></i>
                                            </span>
                                            <span class="text-xs font-semibold text-slate-300">
                                                ${sGroup.toUpperCase()}: <span class="text-emerald-400">${g2Value}</span>
                                            </span>
                                            <span class="text-[10px] text-slate-500">(${g2Tasks.length} Tasks)</span>
                                        </div>
                                    </div>
                                `;

                                const subContentContainer = document.createElement('div');
                                if (isSubCollapsed) {
                                    subContentContainer.className = "hidden";
                                } else {
                                    subContentContainer.appendChild(createBoardStructure(g2Tasks));
                                }

                                subSwimlane.appendChild(subContentContainer);
                                contentContainer.appendChild(subSwimlane);
                            }
                        });
                    } else {
                        contentContainer.appendChild(createBoardStructure(g1Tasks));
                    }
                    
                    swimlaneSection.appendChild(contentContainer);
                    container.appendChild(swimlaneSection);
                });
            }
        }

        function createBoardStructure(boardTasks) {
            const columns = ["ToDo", "Analysis", "InProgress", "Done"];
            const boardGrid = document.createElement('div');
            boardGrid.className = "grid grid-cols-1 xl:grid-cols-4 gap-4 mt-2";

            columns.forEach(col => {
                const colWrapper = document.createElement('div');
                colWrapper.className = "bg-slate-100 dark:bg-slate-950/60 border border-slate-200 dark:border-slate-800/80 rounded-xl p-3 flex flex-col min-h-[220px] transition-all";
                
                const activeFilteredTasks = getFilteredTasks();
                const globalTotalCount = activeFilteredTasks.filter(t => t.column === col).length;

                colWrapper.innerHTML = `
                    <div class="flex justify-between items-center mb-3 px-1">
                        <span class="text-xs font-bold text-slate-600 dark:text-slate-200 tracking-wide uppercase">${getColLabel(col)}</span>
                        <span class="bg-slate-200 dark:bg-slate-900/80 text-[10px] text-slate-500 dark:text-slate-400 px-2.5 py-1 rounded-full border border-slate-300 dark:border-slate-800/50 font-medium">Gesamt: ${globalTotalCount}</span>
                    </div>
                `;

                const subColsContainer = document.createElement('div');
                subColsContainer.className = "grid grid-cols-2 gap-2 flex-1";

                ["Doing", "Done"].forEach(subCol => {
                    const subColBox = document.createElement('div');
                    const globalSubCount = activeFilteredTasks.filter(t => t.column === col && t.subColumn === subCol && !t.isArchived).length;
                    const limit = (subCol === "Doing") ? wipLimits[col] : null;
                    const isOverWip = (limit !== null && limit !== undefined && globalSubCount > limit);

                    let subColBaseClass = "bg-white dark:bg-slate-900/60 border rounded-lg p-2 flex flex-col gap-2 min-h-[140px] transition-all ";
                    subColBaseClass += isOverWip ? "border-red-500/50 bg-red-50/50 dark:bg-red-950/10 shadow-lg shadow-red-950/20" : "border-slate-200 dark:border-slate-800/50";
                    
                    subColBox.className = subColBaseClass;
                    subColBox.setAttribute('data-main', col);
                    subColBox.setAttribute('data-sub', subCol);

                    subColBox.addEventListener('dragover', handleDragOver);
                    subColBox.addEventListener('dragleave', handleDragLeave);
                    subColBox.addEventListener('drop', (e) => handleDrop(e, col, subCol));

                    let subColBadgeHtml = '';
                    if (subCol === "Doing") {
                        const badgeColor = 'bg-amber-500 text-white border-amber-600 dark:bg-amber-500/10 dark:text-amber-400 dark:border-amber-500/20';
                        const badgeInner = `<span class="px-1.5 py-0.5 rounded border font-semibold ${badgeColor}">${subCol}</span>`;
                        
                        let wipLimitPill = '';
                        if (limit !== null && limit !== undefined) {
                            const pillClass = isOverWip 
                                ? 'bg-red-600 text-white border-red-700 dark:bg-red-500/20 dark:text-red-300 dark:border-red-500/40 animate-pulse cursor-pointer hover:bg-red-700 dark:hover:bg-red-500/30' 
                                : 'bg-slate-200 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700/50 cursor-pointer hover:bg-slate-300 dark:hover:bg-slate-700';
                            const warnIcon = isOverWip ? `<i class="fa-solid fa-triangle-exclamation mr-1 text-white dark:text-red-400"></i>` : '';
                            wipLimitPill = `<span onclick="configureWipLimit('${col}')" class="px-1.5 py-0.5 rounded border text-[9px] font-bold ${pillClass}" title="Klicken, um WIP Limit anzupassen">${warnIcon}${globalSubCount}/${limit} WIP</span>`;
                        } else {
                            wipLimitPill = `<span onclick="configureWipLimit('${col}')" class="bg-slate-200 dark:bg-slate-800 text-[9px] text-slate-500 hover:text-slate-600 dark:hover:text-slate-300 px-1.5 py-0.5 rounded border border-slate-300 dark:border-slate-700/50 font-bold cursor-pointer hover:bg-slate-300 dark:hover:bg-slate-700" title="Klicken, um WIP Limit anzupassen">${globalSubCount} (Kein Limit)</span>`;
                        }
                        
                        subColBadgeHtml = `<div class="flex items-center gap-1.5 justify-between w-full">${badgeInner} ${wipLimitPill}</div>`;
                    } else {
                        const badgeColor = 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
                        subColBadgeHtml = `<div class="flex items-center justify-between w-full"><span class="px-1.5 py-0.5 rounded border font-semibold ${badgeColor}">${subCol}</span> <span class="text-[9px] text-slate-400 dark:text-slate-500 font-medium">${globalSubCount} Tasks</span></div>`;
                    }

                    subColBox.innerHTML = `<div class="flex justify-between items-center text-[10px] pb-1 border-b border-slate-200 dark:border-slate-800 mb-1">${subColBadgeHtml}</div>`;

                    const targetTasks = boardTasks.filter(t => t.column === col && t.subColumn === subCol);
                    targetTasks.forEach(task => subColBox.appendChild(createCard(task)));
                    subColsContainer.appendChild(subColBox);
                });

                colWrapper.appendChild(subColsContainer);
                boardGrid.appendChild(colWrapper);
            });

            return boardGrid;
        }

        function createBoardElement(title, boardTasks) {
            const wrapper = document.createElement('div');
            wrapper.innerHTML = `<h2 class="text-sm font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest mb-4"><i class="fa-solid fa-list-check mr-2"></i>${title}</h2>`;
            wrapper.appendChild(createBoardStructure(boardTasks));
            return wrapper;
        }

        function createCard(task) {
            const card = document.createElement('div');
            let cardClasses = "bg-white dark:bg-slate-800/90 border border-slate-200 dark:border-slate-700/60 rounded-lg p-3 shadow-md hover:border-blue-500/50 cursor-pointer transition-all select-none relative group";
            if (task.isArchived) {
                cardClasses += " card-archived";
            }
            card.className = cardClasses;
            card.setAttribute('draggable', !task.isArchived); // Disable drag for archived cards
            card.setAttribute('data-task-id', task.id);
            
            // Use the centrally defined color for the task type
            const typeColor = workflowPaths[task.taskType]?.color || '#64748b'; // Default to gray
            card.style.borderLeft = `4px solid ${typeColor}`;

            card.addEventListener('dragstart', (e) => handleDragStart(e, task.id));
            card.addEventListener('dragend', handleDragEnd);
            card.addEventListener('click', (e) => selectTask(task.id, e));

            let moveOptionsHtml = '';
            if (!task.isArchived) {
                const allowedCols = workflowPaths[task.taskType]?.path || [];
                const moveOptions = [];
                allowedCols.forEach(col => {
                    ["Doing", "Done"].forEach(subCol => {
                        if (!(task.column === col && task.subColumn === subCol)) {
                            moveOptions.push(`<button onclick="event.stopPropagation(); manualMoveTask('${task.id}', '${col}', '${subCol}')" class="w-full text-left px-3 py-1.5 hover:bg-slate-100 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-[11px] font-medium transition-colors">${getColLabel(col)} (${subCol})</button>`);
                        }
                    });
                });
                moveOptionsHtml = `<div class="text-[9px] px-3 py-1 text-slate-500 uppercase font-semibold">Verschieben nach:</div>${moveOptions.join('')}`;
            }

            const archiveButtonHtml = task.isArchived
                ? `<button onclick="event.stopPropagation(); toggleArchiveTask('${task.id}', false)" class="w-full text-left px-3 py-1.5 hover:bg-blue-100 dark:hover:bg-blue-500/20 text-blue-600 dark:text-blue-400 text-[11px] font-medium transition-colors"><i class="fa-solid fa-box-open w-4 mr-1"></i> Wiederherstellen</button>`
                : `<button onclick="event.stopPropagation(); toggleArchiveTask('${task.id}', true)" class="w-full text-left px-3 py-1.5 hover:bg-slate-100 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-[11px] font-medium transition-colors"><i class="fa-solid fa-box-archive w-4 mr-1"></i> Archivieren</button>`;

            let tagsHtml = '';
            if (task.tags && task.tags.length > 0) {
                tagsHtml = `<div class="flex flex-wrap gap-1 mt-2">${task.tags.map(t => {
                    const colHex = tagsConfig[t] || "#475569";
                    return `<span class="border text-[9px] px-1.5 py-0.5 rounded font-bold" style="background-color: ${colHex}20; color: ${colHex}; border-color: ${colHex}40">${t}</span>`;
                }).join('')}</div>`;
            }

            let descriptionIconHtml = '';
            // Check for description content beyond empty tags
            if (task.description && task.description.replace(/<[^>]*>/g, '').trim().length > 0) {
                descriptionIconHtml = `<i class="fa-solid fa-align-left text-slate-400 dark:text-slate-500" title="Diese Aufgabe hat eine Beschreibung"></i>`;
            }

            let linksHtml = '';
            if (task.d365Link) {
                linksHtml += `<a href="${task.d365Link}" target="_blank" title="D365 Task Ã¶ffnen" onclick="event.stopPropagation()" class="text-blue-400 hover:text-blue-300 transition-colors"><i class="fa-solid fa-up-right-from-square text-xs"></i></a>`;
            }
            if (task.timeLink) {
                linksHtml += `<a href="${task.timeLink}" target="_blank" title="Zeiterfassung Ã¶ffnen" onclick="event.stopPropagation()" class="text-emerald-400 hover:text-emerald-300 transition-colors"><i class="fa-solid fa-clock text-xs"></i></a>`;
            }

            card.innerHTML = `
                <div class="flex justify-between items-start gap-1 mb-1.5">
                    <div class="flex items-center gap-1.5">
                        <span class="text-[10px] text-slate-500 dark:text-slate-400 font-bold bg-slate-100 dark:bg-slate-900/60 px-1.5 py-0.5 rounded border border-slate-200 dark:border-slate-700/50">${task.id.slice(0,8)}...</span>
                        <div class="flex items-center gap-2">
                            ${descriptionIconHtml}
                            ${linksHtml}
                        </div>
                    </div>
                    <div class="relative inline-block text-left opacity-0 group-hover:opacity-100 transition-opacity">
                        <button onclick="toggleCardDropdown(event, '${task.id}')" class="text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white px-1"><i class="fa-solid fa-ellipsis-vertical text-xs"></i></button>
                        <div id="drop-${task.id}" class="hidden absolute right-0 mt-1 w-40 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg shadow-xl z-20 py-1">
                            ${moveOptionsHtml}
                            <div class="border-t border-slate-200 dark:border-slate-700/50 my-1"></div>
                            ${archiveButtonHtml}
                            <div class="border-t border-slate-200 dark:border-slate-700/50 my-1"></div>
                            <button onclick="event.stopPropagation(); deleteTask('${task.id}')" class="w-full text-left px-3 py-1.5 hover:bg-red-100 dark:hover:bg-red-500/20 text-red-600 dark:text-red-400 text-[11px] font-medium transition-colors">
                                <i class="fa-solid fa-trash-can w-4 mr-1"></i> EndgÃ¼ltig LÃ¶schen
                            </button>
                        </div>
                    </div>
                </div>
                <h4 class="text-sm font-semibold text-slate-800 dark:text-white leading-tight mb-1 line-clamp-2">${task.title}</h4>
                ${tagsHtml}
                <div class="space-y-1.5 border-t border-slate-200 dark:border-slate-700/40 pt-2 mt-2 text-[10px]">
                    <div class="flex items-center gap-1.5 text-slate-600 dark:text-slate-300"><i class="fa-solid fa-user text-[9px] text-slate-400 dark:text-slate-500"></i><span>Owner: <strong class="text-slate-800 dark:text-white">${task.owner}</strong></span></div>
                    <div class="flex items-center gap-1.5 text-slate-600 dark:text-slate-300"><i class="fa-solid fa-building-user text-[9px] text-slate-400 dark:text-slate-500"></i><span>Kunde: <strong class="text-slate-800 dark:text-white">${task.customer}</strong></span></div>
                    <div class="flex items-center gap-1.5 text-slate-600 dark:text-slate-300"><i class="fa-solid fa-gears text-[9px] text-slate-400 dark:text-slate-500"></i><span>Typ: <strong class="text-slate-800 dark:text-white">${task.taskType}</strong></span></div>
                </div>
            `;
            return card;
        }

        function highlightSelectedCard() {
            document.querySelectorAll('.card-selected').forEach(c => c.classList.remove('card-selected'));
            if (selectedTaskId) {
                const card = document.querySelector(`[data-task-id="${selectedTaskId}"]`);
                if (card) card.classList.add('card-selected');
            }
        }

        function getFilteredTasks() {
            const showArchived = document.getElementById('filter-archived').checked;
            return tasks.filter(task => {
                const oMatch = filterOwner.value === 'ALL' || task.owner === filterOwner.value;
                const cMatch = filterCustomer.value === 'ALL' || task.customer === filterCustomer.value;
                const tMatch = filterType.value === 'ALL' || task.taskType === filterType.value;
                const archiveMatch = showArchived ? task.isArchived === true : task.isArchived !== true;
                return oMatch && cMatch && tMatch && archiveMatch;
            });
        }

        // --- EVENT HANDLERS & MODAL LOGIC ---
        // Note: Functions called by `onclick` are attached to `window`
        window.toggleSwimlane = function(swimlaneId) {
            if (collapsedSwimlanes.has(swimlaneId)) {
                collapsedSwimlanes.delete(swimlaneId);
            } else {
                collapsedSwimlanes.add(swimlaneId);
            }
            renderBoard();
        }

        window.configureWipLimit = function(col) {
            const currentLimit = wipLimits[col];
            const input = prompt(`Neues WIP Limit fÃ¼r die Subspalte "${getColLabel(col)} (Doing)" eingeben (Leer oder 0 zum Deaktivieren):`, currentLimit !== null ? currentLimit : "");
            if (input === null) return;
            const num = parseInt(input.trim(), 10);
            if (input.trim() === "" || isNaN(num) || num <= 0) {
                wipLimits[col] = null;
                showToast(`WIP Limit fÃ¼r "${getColLabel(col)} (Doing)" wurde aufgehoben.`);
            } else {
                wipLimits[col] = num;
                showToast(`WIP Limit fÃ¼r "${getColLabel(col)} (Doing)" auf ${num} gesetzt.`);
            }
            saveAllState();
            renderBoard();
        }

        function validateWorkflowMove(taskId, targetColumn) {
            const task = tasks.find(t => t.id === taskId);
            if (!task) return false;

            const allowedColumns = workflowPaths[task.taskType]?.path;
            if (!allowedColumns || !allowedColumns.includes(targetColumn)) {
                showToast(`Fehler: Der Typ "${task.taskType}" darf die Spalte "${getColLabel(targetColumn)}" nicht nutzen!`);
                return false;
            }

            if (task.column !== targetColumn && wipLimits[targetColumn] !== null) {
                const currentGlobalCount = tasks.filter(t => t.column === targetColumn && t.subColumn === "Doing").length;
                if (currentGlobalCount >= wipLimits[targetColumn]) {
                    showToast(`Achtung: WIP Limit fÃ¼r "${getColLabel(targetColumn)} (Doing)" (${wipLimits[targetColumn]}) wird Ã¼berschritten!`);
                }
            }
            return true;
        }

        window.toggleArchiveTask = async function(taskId, shouldArchive) {
            const taskTitle = tasks.find(t => t.id === taskId)?.title || 'diesen Task';
            const actionText = shouldArchive ? 'archivieren' : 'wiederherstellen';

            if (confirm(`MÃ¶chten Sie "${taskTitle}" wirklich ${actionText}?`)) {
                if (selectedTaskId === taskId) {
                    closeDetailsPanel();
                }
                const success = await updateTaskInCloud(taskId, { isArchived: shouldArchive });
                if (!success) return;
                showToast(`Task wurde ${actionText}.`);
                await loadTasksFromCloud();
            }
        }

        window.deleteTask = async function(taskId) {
            const taskTitle = tasks.find(t => t.id === taskId)?.title || 'diesen Task';
            if (!confirm(`MÃ¶chten Sie "${taskTitle}" endgÃ¼ltig lÃ¶schen?`)) {
                return;
            }

            const success = await deleteTaskFromCloud(taskId);
            if (!success) return;
            showToast('Task wurde endgÃ¼ltig gelÃ¶scht.');
            await loadTasksFromCloud();
        }
        // Drag & Drop Handlers
        function handleDragStart(e, taskId) {
            draggedTaskId = taskId;
            e.dataTransfer.effectAllowed = "move";
            e.target.style.opacity = "0.5";
            
            const task = tasks.find(t => t.id === taskId);
            if (task) {
                const allowedCols = workflowPaths[task.taskType]?.path || [];
                document.querySelectorAll('[data-main]').forEach(el => {
                    if (allowedCols.includes(el.dataset.main)) {
                        el.classList.add('column-allowed');
                    } else {
                        el.classList.add('column-disabled');
                    }
                });
            }
        }

        function handleDragEnd(e) {
            e.target.style.opacity = "1";
            document.querySelectorAll('.column-disabled, .column-allowed, .drag-over').forEach(el => {
                el.classList.remove('column-disabled', 'column-allowed', 'drag-over');
            });
        }

        function handleDragOver(e) {
            if (e.target.closest('.column-disabled')) return;
            e.preventDefault();
            const dropzone = e.target.closest('[data-main]');
            if (dropzone) dropzone.classList.add('drag-over');
        }

        function handleDragLeave(e) {
            const dropzone = e.target.closest('[data-main]');
            if (dropzone) dropzone.classList.remove('drag-over');
        }

        window.handleDrop = async function(e, mainCol, subCol) {
            e.preventDefault();
            document.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over'));
            if (!draggedTaskId) return;

            if (validateWorkflowMove(draggedTaskId, mainCol)) {
                const success = await updateTaskInCloud(draggedTaskId, { column: mainCol, subColumn: subCol });
                if (!success) {
                    draggedTaskId = null;
                    return;
                }
                await loadTasksFromCloud();
            }
            draggedTaskId = null;
        }

        window.manualMoveTask = async function(taskId, mainCol, subCol) {
            if (validateWorkflowMove(taskId, mainCol)) {
                const success = await updateTaskInCloud(taskId, { column: mainCol, subColumn: subCol });
                if (success) {
                    await loadTasksFromCloud();
                }
            }
        }

        window.toggleCardDropdown = function(event, taskId) {
            event.stopPropagation();
            const dropdown = document.getElementById(`drop-${taskId}`);
            const isHidden = dropdown.classList.contains('hidden');
            document.querySelectorAll('[id^="drop-"]').forEach(el => el.classList.add('hidden'));
            if (isHidden) dropdown.classList.remove('hidden');
        }

        // Form & Modal Handlers
        window.handleFormSubmit = async function(e) {
            e.preventDefault();
            const form = e.target;
            const taskId = form.querySelector('#modal-task-id').value;
            const taskData = {
                title: form.querySelector('#modal-title').value.trim(),
                taskType: form.querySelector('#modal-type').value,
                owner: form.querySelector('#modal-owner').value,
                customer: form.querySelector('#modal-customer').value,
                d365Link: form.querySelector('#modal-d365-link').value,
                timeLink: form.querySelector('#modal-time-link').value,
                description: form.querySelector('#modal-description').innerHTML,
                tags: [...selectedModalTags]
            };

            if (taskId) {
                const task = tasks.find(t => t.id === taskId);
                if (task && task.taskType !== taskData.taskType) {
                    const allowed = workflowPaths[taskData.taskType]?.path;
                    if (!allowed || !allowed.includes(task.column)) {
                        showToast(`Ã„nderung des Typs verweigert: Die aktuelle Spalte "${getColLabel(task.column)}" ist fÃ¼r den neuen Typ nicht zulÃ¤ssig!`);
                        return;
                    }
                }
                const success = await updateTaskInCloud(taskId, taskData);
                if (!success) return;
                showToast(`Task ${taskId.slice(0,8)}... wurde aktualisiert.`);
            } else {
                if (wipLimits["ToDo"] !== null) {
                    const currentGlobalCount = tasks.filter(t => t.column === "ToDo" && t.subColumn === "Doing").length;
                    if (currentGlobalCount >= wipLimits["ToDo"]) {
                        showToast(`Achtung: WIP Limit fÃ¼r "To Do (Doing)" wird durch den neuen Task Ã¼berschritten!`);
                    }
                }
                taskData.column = "ToDo";
                taskData.isArchived = false;
                taskData.subColumn = "Doing";
                const newId = await addTaskToCloud(taskData);
                if (!newId) return;
                showToast(`Task ${newId.slice(0,8)}... wurde im To Do erstellt.`);
            }

            closeAddTaskModal();
            await loadTasksFromCloud();
        }

        window.openAddTaskModal = function() {
            document.getElementById('panel-heading').innerHTML = `<i class="fa-solid fa-square-plus text-blue-500"></i> Neuen Task erstellen`;
            document.getElementById('task-form').reset();
            document.getElementById('modal-task-id').value = "";
            selectedTaskId = null;
            document.getElementById('modal-description').innerHTML = "";
            selectedModalTags = [];
            updateAllDropdowns();
            setupOwnerCombobox();
            setupCustomerCombobox();
            renderModalTagsSelection();
            openDetailsPanel();
        }

        window.openEditTaskModal = function(taskId, event) {
            if (event && event.target.closest('button, a')) return;
            const task = tasks.find(t => t.id === taskId);
            if (!task) return;

            document.getElementById('panel-heading').innerHTML = `<i class="fa-solid fa-pen-to-square text-blue-500"></i> Task bearbeiten (${task.id.slice(0,8)}...)`;
            const form = document.getElementById('task-form');
            form.querySelector('#modal-task-id').value = task.id;
            form.querySelector('#modal-title').value = task.title;
            form.querySelector('#modal-type').value = task.taskType;
            form.querySelector('#modal-owner').value = task.owner;
            form.querySelector('#modal-customer').value = task.customer;
            form.querySelector('#modal-d365-link').value = task.d365Link || "";
            form.querySelector('#modal-time-link').value = task.timeLink || "";
            form.querySelector('#modal-description').innerHTML = task.description || "";
            
            selectedModalTags = task.tags ? [...task.tags] : [];
            updateAllDropdowns();
            setupOwnerCombobox();
            setupCustomerCombobox();
            renderModalTagsSelection();
        }

        window.selectTask = function(taskId, event) {
            if (event && event.target.closest('button, a')) return;
            
            selectedTaskId = taskId;
            highlightSelectedCard();
            openEditTaskModal(taskId, event); // Re-use the logic to fill the form
            openDetailsPanel();
        }

        function openDetailsPanel() {
            document.getElementById('task-details-panel').classList.remove('hidden');
            document.getElementById('task-details-panel').classList.add('flex');
            document.getElementById('details-resizer').classList.remove('hidden');
            highlightSelectedCard();
        }

        window.closeDetailsPanel = function() {
            document.getElementById('task-details-panel').classList.add('hidden');
            document.getElementById('task-details-panel').classList.remove('flex');
            document.getElementById('details-resizer').classList.add('hidden');
            selectedTaskId = null;
            highlightSelectedCard();
        }

        window.closeAddTaskModal = function() {
            // This function now closes the panel
            closeDetailsPanel();
        }

        window.formatDoc = function(command, value = null) {
            document.execCommand(command, false, value);
        }

        // Tag & Type Config Modals
        window.openTagsModal = function() {
            renderTagsManagerList();
            document.getElementById('tags-modal').classList.remove('hidden');
        }
        window.closeTagsModal = function() { document.getElementById('tags-modal').classList.add('hidden'); }

        window.openTypesModal = function() {
            renderTypesManagerList();
            document.getElementById('types-modal').classList.remove('hidden');
        }
        window.closeTypesModal = function() { document.getElementById('types-modal').classList.add('hidden'); }

        function renderModalTagsSelection() {
            const container = document.getElementById('modal-tags-selection');
            container.innerHTML = Object.keys(tagsConfig).map(tag => {
                const isSelected = selectedModalTags.includes(tag);
                const color = tagsConfig[tag];
                const activeStyle = isSelected 
                    ? `style="background-color: ${color}; color: #ffffff; border-color: ${color}"`
                    : `style="background-color: ${color}10; color: ${color}; border-color: ${color}30"`;
                return `<button type="button" onclick="toggleModalTag('${tag}')" class="text-[11px] px-2.5 py-1 rounded font-semibold border transition-all duration-200" ${activeStyle}>${tag} ${isSelected ? 'âœ“' : ''}</button>`;
            }).join('');
        }

        window.toggleModalTag = function(tag) {
            if (selectedModalTags.includes(tag)) {
                selectedModalTags = selectedModalTags.filter(t => t !== tag);
            } else {
                selectedModalTags.push(tag);
            }
            renderModalTagsSelection();
        }

        function renderTagsManagerList() {
            document.getElementById('tags-manager-list').innerHTML = Object.keys(tagsConfig).map(tag => `
                <div class="flex items-center justify-between bg-slate-850 p-2.5 border border-slate-800 rounded-lg">
                    <span class="text-xs px-2.5 py-1 rounded font-bold border" style="background-color: ${tagsConfig[tag]}20; border-color: ${tagsConfig[tag]}50; color: ${tagsConfig[tag]}">${tag}</span>
                    <div class="flex items-center gap-2">
                        <input type="color" onchange="updateTagColor('${tag}', this.value)" value="${tagsConfig[tag]}" class="w-8 h-6 bg-slate-800 border border-slate-700 rounded cursor-pointer">
                        <button onclick="deleteTag('${tag}')" class="text-slate-500 hover:text-red-500 text-xs px-2 py-1"><i class="fa-solid fa-trash"></i></button>
                    </div>
                </div>`).join('');
        }

        window.addNewTag = function() {
            const nameInput = document.getElementById('new-tag-name');
            const name = nameInput.value.trim();
            if (!name) return;
            tagsConfig[name] = document.getElementById('new-tag-color').value;
            nameInput.value = "";
            saveAllState();
            renderTagsManagerList();
            renderBoard();
        }

        window.updateTagColor = function(tag, newColor) {
            tagsConfig[tag] = newColor;
            saveAllState();
            renderBoard();
        }

        window.deleteTag = async function(tag) {
            const tasksToUpdate = tasks.filter(task => task.tags && task.tags.includes(tag));
            delete tagsConfig[tag];
            const updatePromises = tasksToUpdate.map(task => {
                const newTags = task.tags.filter(t => t !== tag);
                return updateTaskInCloud(task.id, { tags: newTags });
            });
            await Promise.all(updatePromises);
            saveAllState();
            renderTagsManagerList(); // Visuelle Liste im Modal
            // loadTasksFromCloud wird die Dropdowns nach dem Neuladen der Daten aktualisieren
            await loadTasksFromCloud();
        }

        function renderTypesManagerList() {
            document.getElementById('types-manager-list').innerHTML = Object.keys(workflowPaths).map(type => {
                const typeConfig = workflowPaths[type];
                const cols = typeConfig.path.map(c => getColLabel(c)).join(', ');
                return `
                <div class="flex items-center justify-between bg-slate-850 p-2.5 border border-slate-800 rounded-lg">
                        <div class="flex items-center gap-3">
                            <span class="w-3 h-3 rounded-full" style="background-color: ${typeConfig.color}"></span>
                            <div class="flex-1">
                                <span class="text-xs font-bold text-slate-800 dark:text-white block">${type}</span>
                                <span class="text-[10px] text-slate-500 dark:text-slate-400">Pfad: ${cols}</span>
                            </div>
                        </div>
                        <div class="flex items-center gap-2">
                            <input type="color" onchange="updateTypeColor('${type}', this.value)" value="${typeConfig.color}" class="w-8 h-6 bg-transparent dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded cursor-pointer">
                            <button onclick="deleteType('${type}')" class="text-slate-500 hover:text-red-600 dark:hover:text-red-500 text-xs px-2 py-1"><i class="fa-solid fa-trash"></i></button>
                        </div>
                    </div>`;
            }).join('');
        }

        window.addNewType = function() {
            const nameInput = document.getElementById('new-type-name');
            const name = nameInput.value.trim();
            const color = document.getElementById('new-type-color').value;
            if (!name) return;
            const path = ["ToDo"];
            if (document.getElementById('path-analysis').checked) path.push("Analysis");
            if (document.getElementById('path-inprogress').checked) path.push("InProgress");
            path.push("Done");
            workflowPaths[name] = { path, color };
            nameInput.value = "";
            saveAllState();
            updateConfiguredDropdowns();
            renderTypesManagerList();
            renderBoard();
        }

        window.updateTypeColor = function(type, newColor) {
            if (workflowPaths[type]) {
                workflowPaths[type].color = newColor;
            }
            saveAllState();
            updateConfiguredDropdowns();
            renderTypesManagerList();
            renderBoard();
        }

        window.deleteType = async function(type) {
            if (Object.keys(workflowPaths).length <= 1) {
                return showToast("Mindestens ein Task-Typ muss auf dem Board erhalten bleiben!");
            }
            const tasksToDelete = tasks.filter(task => task.taskType === type);
            await Promise.all(tasksToDelete.map(task => deleteTaskFromCloud(task.id)));
            delete workflowPaths[type];
            saveAllState();
            updateAllDropdowns();
            renderTypesManagerList();
            await loadTasksFromCloud();
        }

        function updateConfiguredDropdowns() {
            const filterTypeSelect = document.getElementById('filter-type');
            const modalTypeSelect = document.getElementById('modal-type');
            const selectedFilterVal = filterTypeSelect.value || "ALL";
            const selectedModalVal = modalTypeSelect.value || "";
            let filterHtml = `<option value="ALL">Alle Typen</option>`;
            let modalHtml = ``;
            Object.keys(workflowPaths).forEach(type => {
                filterHtml += `<option value="${type}">${type}</option>`;
                modalHtml += `<option value="${type}">${type}</option>`;
            });
            filterTypeSelect.innerHTML = filterHtml;
            modalTypeSelect.innerHTML = modalHtml;
            filterTypeSelect.value = selectedFilterVal;
            if (selectedModalVal && workflowPaths[selectedModalVal]) {
                modalTypeSelect.value = selectedModalVal;
            }
            const legendContainer = document.getElementById('dynamic-legend');
            legendContainer.innerHTML = "";
            Object.keys(workflowPaths).forEach(type => {
                const color = workflowPaths[type].color || '#64748b';
                legendContainer.innerHTML += `<div class="flex items-center gap-1.5"><span class="w-2.5 h-2.5 rounded-full" style="background-color: ${color}"></span><span><strong>${type}:</strong> (${workflowPaths[type].path.map(c => getColLabel(c)).join(' âž” ')})</span></div>`;
            });
        }

        function updateDynamicListsFromTasks(currentTasks) {
            // Leitet die Listen fÃ¼r Owner und Kunden direkt aus den vorhandenen Aufgaben ab.
            const ownerSet = new Set();
            const customerSet = new Set();
            currentTasks.forEach(task => {
                if (task.owner) ownerSet.add(task.owner);
                if (task.customer) customerSet.add(task.customer);
            });
            owners = [...ownerSet].sort();
            customers = [...customerSet].sort();
        }

        function updateOwnerDropdowns() {
            const filterOwnerSelect = document.getElementById('filter-owner');
            const modalOwnerDatalist = document.getElementById('owner-list');
            
            const savedFilterValue = filterOwnerSelect.value;

            // Filter-Dropdown aktualisieren
            filterOwnerSelect.innerHTML = `<option value="ALL">Alle Owner</option>`;
            owners.forEach(owner => {
                filterOwnerSelect.innerHTML += `<option value="${owner}">${owner}</option>`;
            });
            filterOwnerSelect.value = savedFilterValue;

            // Datalist im Modal aktualisieren
            modalOwnerDatalist.innerHTML = '';
            owners.forEach(owner => {
                modalOwnerDatalist.innerHTML += `<option value="${owner}"></option>`;
            });
        }

        function updateCustomerDropdowns() {
            const filterCustomerSelect = document.getElementById('filter-customer');
            const modalCustomerDatalist = document.getElementById('customer-list');
            
            const savedFilterValue = filterCustomerSelect.value;

            // Filter-Dropdown aktualisieren
            filterCustomerSelect.innerHTML = `<option value="ALL">Alle Kunden</option>`;
            customers.forEach(customer => {
                filterCustomerSelect.innerHTML += `<option value="${customer}">${customer}</option>`;
            });
            filterCustomerSelect.value = savedFilterValue;

            // Datalist im Modal aktualisieren
            modalCustomerDatalist.innerHTML = '';
            customers.forEach(customer => {
                modalCustomerDatalist.innerHTML += `<option value="${customer}"></option>`;
            });
        }

        function updateAllDropdowns() {
            updateConfiguredDropdowns(); // For Task Types
            updateOwnerDropdowns();
            updateCustomerDropdowns();
        }

        function setupCombobox(containerSelector, inputId, dropdownId, arrowId, dataArray) {
            const container = document.querySelector(containerSelector);
            if (!container) return; // Guard against missing elements
            const input = document.getElementById(inputId);
            const customDropdown = document.getElementById(dropdownId);
            const arrow = document.getElementById(arrowId);

            const renderItems = (filter = '') => {
                const filteredItems = dataArray.filter(item => item.toLowerCase().includes(filter.toLowerCase()));
                if (filteredItems.length === 0) {
                    customDropdown.classList.add('hidden');
                    return;
                }
                customDropdown.innerHTML = filteredItems.map(item => 
                    `<div class="combobox-item text-sm text-slate-200 hover:bg-blue-600" data-value="${item}">${item}</div>`
                ).join('');
                customDropdown.classList.remove('hidden');
            };

            const toggleDropdown = () => {
                if (customDropdown.classList.contains('hidden')) {
                    renderItems(input.value);
                } else {
                    customDropdown.classList.add('hidden');
                }
            };

            input.addEventListener('focus', () => renderItems(''));
            input.addEventListener('input', () => renderItems(input.value));
            arrow.addEventListener('click', (e) => {
                e.stopPropagation();
                toggleDropdown();
                input.focus();
            });

            customDropdown.addEventListener('click', (e) => {
                if (e.target.classList.contains('combobox-item')) {
                    input.value = e.target.dataset.value;
                    customDropdown.classList.add('hidden');
                }
            });

            document.addEventListener('click', (e) => {
                if (!container.contains(e.target)) {
                    customDropdown.classList.add('hidden');
                }
            });
        }

        function setupCustomerCombobox() {
            setupCombobox('#modal-customer-container', 'modal-customer', 'customer-custom-dropdown', 'customer-dropdown-arrow', customers);
        }

        function setupOwnerCombobox() {
            setupCombobox('#modal-owner-container', 'modal-owner', 'owner-custom-dropdown', 'owner-dropdown-arrow', owners);
        }

        // --- INITIALIZATION ---
        document.addEventListener('DOMContentLoaded', async () => {
            // Add event listeners for controls
            groupPrimary.addEventListener('change', () => {
                if (groupPrimary.value === 'NONE') {
                    groupSecondary.value = 'NONE';
                    groupSecondary.disabled = true;
                } else {
                    groupSecondary.disabled = false;
                    Array.from(groupSecondary.options).forEach(opt => {
                        opt.disabled = (opt.value === groupPrimary.value && opt.value !== 'NONE');
                    });
                }
                collapsedSwimlanes.clear();
                renderBoard();
            });
            groupSecondary.addEventListener('change', () => { collapsedSwimlanes.clear(); renderBoard(); });
            filterOwner.addEventListener('change', renderBoard);
            filterCustomer.addEventListener('change', renderBoard);
            filterType.addEventListener('change', renderBoard);
            document.getElementById('filter-archived').addEventListener('change', renderBoard);
            window.addEventListener('click', () => {
                document.querySelectorAll('[id^="drop-"]').forEach(el => el.classList.add('hidden'));
            });

            await setupFirestorePersistence();

            // Initial data load and render
            updateAllDropdowns();
            setupOwnerCombobox();
            setupCustomerCombobox(); // Initial setup for event listeners
            await loadTasksFromCloud();

            // --- Theme Toggle Logic ---
            const themeToggleDarkIcon = document.getElementById('theme-toggle-dark-icon');
            const themeToggleLightIcon = document.getElementById('theme-toggle-light-icon');

            // Change the icons inside the button based on previous settings
            if (localStorage.getItem('theme') === 'dark' || (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
                themeToggleLightIcon.classList.remove('hidden');
            } else {
                themeToggleDarkIcon.classList.remove('hidden');
            }

            const themeToggleBtn = document.getElementById('theme-toggle');

            themeToggleBtn.addEventListener('click', function() {
                // toggle icons inside button
                themeToggleDarkIcon.classList.toggle('hidden');
                themeToggleLightIcon.classList.toggle('hidden');

                // if set via local storage previously
                if (localStorage.getItem('theme')) {
                    if (localStorage.getItem('theme') === 'light') {
                        document.documentElement.classList.add('dark');
                        localStorage.setItem('theme', 'dark');
                    } else {
                        document.documentElement.classList.remove('dark');
                        localStorage.setItem('theme', 'light');
                    }
                // if NOT set via local storage previously
                } else {
                    if (document.documentElement.classList.contains('dark')) {
                        document.documentElement.classList.remove('dark');
                        localStorage.setItem('theme', 'light');
                    } else {
                        document.documentElement.classList.add('dark');
                        localStorage.setItem('theme', 'dark');
                    }
                }
            });
        });

        // --- Resizable Panel Logic ---
        const resizer = document.getElementById('details-resizer');
        const panel = document.getElementById('task-details-panel');
        let isResizing = false;

        // Load saved panel width
        const savedWidth = localStorage.getItem('kanban_panel_width');
        if (savedWidth) {
            panel.style.width = `${savedWidth}px`;
        }

        resizer.addEventListener('mousedown', (e) => {
            isResizing = true;
            document.body.classList.add('is-resizing');
            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', stopResize);
        });

        function handleMouseMove(e) {
            if (!isResizing) return;
            const newWidth = window.innerWidth - e.clientX;
            if (newWidth > 350 && newWidth < 800) { // Min and max width
                panel.style.width = `${newWidth}px`;
            }
        }

        function stopResize() {
            isResizing = false;
            document.body.classList.remove('is-resizing');
            localStorage.setItem('kanban_panel_width', panel.offsetWidth);
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', stopResize);
        }
    
