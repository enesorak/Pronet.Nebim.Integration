// Global variables
let currentEditingDevice = null;
let devices = [];
let updateLogs = [];

// API Configuration
const API_BASE_URL = ''; // Backend URL (boş bırakılırsa aynı domain'i kullanır)

// API Helper Functions
async function apiRequest(url, options = {}) {
    try {
        const response = await fetch(API_BASE_URL + url, {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        // NoContent (204) response için boş döndür
        if (response.status === 204) {
            return null;
        }

        // Response'un content-type'ını kontrol et
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            return await response.json();
        } else {
            // JSON değilse text olarak döndür
            const text = await response.text();
            return text || null;
        }
    } catch (error) {
        console.error('API Error:', error);
        throw error;
    }
}

// API Functions
async function loadSettings() {
    try {
        const settings = await apiRequest('/api/settings');

        // Settings formunu doldur
        if (settings['ApiUrls:Pronet']) document.getElementById('pronet-api').value = settings['ApiUrls:Pronet'];
        if (settings['Credentials:Pronet:UserName']) document.getElementById('pronet-username').value = settings['Credentials:Pronet:UserName'];
        if (settings['Credentials:Pronet:Password']) document.getElementById('pronet-password').value = "*****";
        if (settings['Credentials:Pronet:PronetMode']) document.getElementById('pronet-mode').value = settings['Credentials:Pronet:PronetMode'];

        if (settings['ApiUrls:Nebim']) document.getElementById('nebim-api').value = settings['ApiUrls:Nebim'];
        if (settings['Credentials:Nebim:UserName']) document.getElementById('nebim-username').value = settings['Credentials:Nebim:UserName'];
        if (settings['Credentials:Nebim:Password']) document.getElementById('nebim-password').value = settings['Credentials:Nebim:Password'];

        if (settings['Scheduler:FrequencyMinutes']) document.getElementById('sync-frequency').value = settings['Scheduler:FrequencyMinutes'];

        console.log('Ayarlar yüklendi:', settings);
    } catch (error) {
        console.error('Ayarlar yüklenirken hata:', error);
        showNotification('Ayarlar yüklenirken hata oluştu!', 'error');
    }
}

async function saveSettings() {
    try {
        const settings = {
            'ApiUrls:Pronet': document.getElementById('pronet-api').value,
            'Credentials:Pronet:UserName': document.getElementById('pronet-username').value,
            'Credentials:Pronet:Password': document.getElementById('pronet-password').value,
            'Credentials:Pronet:PronetMode': document.getElementById('pronet-mode').value,
            'ApiUrls:Nebim': document.getElementById('nebim-api').value,
            'Credentials:Nebim:UserName': document.getElementById('nebim-username').value,
            'Credentials:Nebim:Password': document.getElementById('nebim-password').value,
            'Scheduler:FrequencyMinutes': document.getElementById('sync-frequency').value
        };

        console.log('Kaydedilecek ayarlar:', settings); // Debug log

        const result = await apiRequest('/api/settings', {
            method: 'POST',
            body: JSON.stringify(settings)
        });

        console.log('API response:', result); // Debug log
        console.log('Ayarlar başarıyla kaydedildi');
        return true;
    } catch (error) {
        console.error('Ayarlar kaydedilirken hata:', error);
        throw error;
    }
}

async function loadDevices() {
    try {
        devices = await apiRequest('/api/devices');
        console.log('Cihazlar yüklendi:', devices);
        renderDeviceList();
    } catch (error) {
        console.error('Cihazlar yüklenirken hata:', error);
        showNotification('Cihazlar yüklenirken hata oluştu!', 'error');
    }
}

async function saveDevice(deviceData) {
    try {
        // API modeline uygun formata çevir
        const device = {
            IsActive: deviceData.active,
            NebimOfficeCode: deviceData.nebimOfficeCode,
            NebimStoreCode: deviceData.nebimStoreCode,
            PronetStoreCode: deviceData.pronetStoreCode || null,
            MacAddress: deviceData.deviceMacAddress || null,
            OpeningTime: deviceData.openingTime, // TimeOnly format (HH:MM)
            ClosingTime: deviceData.closingTime   // TimeOnly format (HH:MM)
        };

        if (currentEditingDevice) {
            // Güncelleme
            await apiRequest(`/api/devices/${currentEditingDevice}`, {
                method: 'PUT',
                body: JSON.stringify(device)
            });
            console.log('Cihaz güncellendi');
        } else {
            // Yeni ekleme
            await apiRequest('/api/devices', {
                method: 'POST',
                body: JSON.stringify(device)
            });
            console.log('Yeni cihaz eklendi');
        }

        await loadDevices(); // Listeyi yenile
        return true;
    } catch (error) {
        console.error('Cihaz kaydedilirken hata:', error);
        throw error;
    }
}

async function deleteDeviceAPI(deviceId) {
    try {
        await apiRequest(`/api/devices/${deviceId}`, {
            method: 'DELETE'
        });
        console.log('Cihaz silindi');
        await loadDevices(); // Listeyi yenile
        return true;
    } catch (error) {
        console.error('Cihaz silinirken hata:', error);
        throw error;
    }
}

async function testPronetConnection() {
    try {
        const response = await apiRequest('/api/test/pronet', {
            method: 'POST'
        });
        console.log('Pronet test başarılı:', response);
        return true;
    } catch (error) {
        console.error('Pronet test hatası:', error);
        throw error;
    }
}

async function testNebimConnection() {
    try {
        const response = await apiRequest('/api/test/nebim', {
            method: 'POST'
        });
        console.log('Nebim test başarılı:', response);
        return true;
    } catch (error) {
        console.error('Nebim test hatası:', error);
        throw error;
    }
 
try {
    updateLogs = await apiRequest('/api/dashboard/status');
    console.log('Dashboard durumu yüklendi:', updateLogs);
    renderUpdateLogs();
    updateStats();
} catch (error) {
    console.error('Dashboard durumu yüklenirken hata:', error);
    showNotification('Dashboard verileri yüklenirken hata oluştu!', 'error');
}
}

// Notification Helper
function showNotification(message, type = 'info') {
    // Basit notification sistemi - daha sonra geliştirilebir
    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 p-4 rounded-lg text-white z-50 ${
        type === 'error' ? 'bg-red-500' :
            type === 'success' ? 'bg-green-500' : 'bg-blue-500'
    }`;
    notification.textContent = message;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.remove();
    }, 3000);
}

// Navigation Functions
function showDashboard() {
    console.log('Dashboard gösteriliyor');
    document.getElementById('deviceManagementSection').style.display = 'none';
    document.getElementById('settingsSection').classList.add('hidden');
    document.getElementById('dashboardSection').classList.remove('hidden');

    // Update navigation
    updateNavigation('dashboard');

    // Update URL hash
    window.location.hash = 'dashboard';

    // Load dashboard data
    loadDashboardStatus();
}

function showSettings() {
    console.log('Settings gösteriliyor');
    document.getElementById('deviceManagementSection').style.display = 'none';
    document.getElementById('dashboardSection').classList.add('hidden');
    document.getElementById('settingsSection').classList.remove('hidden');

    // Update navigation
    updateNavigation('settings');

    // Update URL hash
    window.location.hash = 'settings';

    // Load settings
    loadSettings();
}

function showDeviceManagement() {
    console.log('Device Management gösteriliyor');
    document.getElementById('deviceManagementSection').style.display = 'block';
    document.getElementById('settingsSection').classList.add('hidden');
    document.getElementById('dashboardSection').classList.add('hidden');

    // Update navigation
    updateNavigation('devices');

    // Update URL hash
    window.location.hash = 'devices';

    // Load devices
    loadDevices();
}

function updateNavigation(activeTab) {
    // Reset all navigation links
    document.querySelectorAll('nav a').forEach(link => {
        link.className = 'inline-flex items-center px-3 py-2 text-sm font-medium text-gray-500 hover:text-gray-700 hover:bg-gray-50 rounded-md transition-colors';
    });

    // Set active navigation link
    const navMap = {
        'dashboard': 'nav a[href="#dashboard"]',
        'devices': 'nav a[href="#devices"]',
        'settings': 'nav a[href="#settings"]'
    };

    const activeLink = document.querySelector(navMap[activeTab]);
    if (activeLink) {
        activeLink.className = 'inline-flex items-center px-3 py-2 text-sm font-medium text-primary bg-blue-50 rounded-md';
    }
}

function handleHashChange() {
    const fullHash = window.location.hash;
    const hash = fullHash.replace('#', '') || 'dashboard';

    console.log('Full URL:', window.location.href);
    console.log('Full hash:', fullHash);
    console.log('Parsed hash:', hash);

    switch(hash) {
        case 'dashboard':
            console.log('Switching to dashboard');
            showDashboardOnly();
            break;
        case 'devices':
            console.log('Switching to devices');
            showDeviceManagementOnly();
            break;
        case 'settings':
            console.log('Switching to settings');
            showSettingsOnly();
            break;
        default:
            console.log('Unknown hash, defaulting to dashboard');
            showDashboardOnly();
            break;
    }
}

// Hash değiştirmeden sayfa gösterme fonksiyonları
function showDashboardOnly() {
    console.log('Dashboard gösteriliyor (hash değişmeden)');
    document.getElementById('deviceManagementSection').style.display = 'none';
    document.getElementById('settingsSection').classList.add('hidden');
    document.getElementById('dashboardSection').classList.remove('hidden');

    updateNavigation('dashboard');
    loadDashboardStatus();
}

function showSettingsOnly() {
    console.log('Settings gösteriliyor (hash değişmeden)');
    document.getElementById('deviceManagementSection').style.display = 'none';
    document.getElementById('dashboardSection').classList.add('hidden');
    document.getElementById('settingsSection').classList.remove('hidden');

    updateNavigation('settings');
    loadSettings();
}

function showDeviceManagementOnly() {
    console.log('Device Management gösteriliyor (hash değişmeden)');
    document.getElementById('deviceManagementSection').style.display = 'block';
    document.getElementById('settingsSection').classList.add('hidden');
    document.getElementById('dashboardSection').classList.add('hidden');

    updateNavigation('devices');
    loadDevices();
}

// Device Management Functions
function openAddDeviceModal() {
    currentEditingDevice = null;
    document.getElementById('modalTitle').textContent = 'Yeni Cihaz Ekle';
    document.getElementById('deviceForm').reset();
    document.getElementById('deviceActive').checked = true;
    document.getElementById('openingTime').value = '09:00';
    document.getElementById('closingTime').value = '22:00';
    document.getElementById('deviceModal').classList.remove('hidden');
}

function closeDeviceModal() {
    document.getElementById('deviceModal').classList.add('hidden');
}

function editDevice(deviceId) {
    const device = devices.find(d => d.Id === deviceId);
    if (!device) return;

    currentEditingDevice = deviceId;
    document.getElementById('modalTitle').textContent = 'Cihaz Düzenle';

    // TimeOnly formatını düzenle
    const formatTimeForInput = (timeString) => {
        if (!timeString) return '09:00';
        // Eğer "HH:MM:SS" formatındaysa sadece "HH:MM" kısmını al
        return timeString.length > 5 ? timeString.substring(0, 5) : timeString;
    };

    document.getElementById('deviceActive').checked = device.IsActive;
    document.getElementById('nebimOfficeCode').value = device.NebimOfficeCode;
    document.getElementById('nebimStoreCode').value = device.NebimStoreCode;
    document.getElementById('pronetStoreCode').value = device.PronetStoreCode || '';
    document.getElementById('deviceMacAddress').value = device.MacAddress || '';
    document.getElementById('openingTime').value = formatTimeForInput(device.OpeningTime);
    document.getElementById('closingTime').value = formatTimeForInput(device.ClosingTime);

    document.getElementById('deviceModal').classList.remove('hidden');
}

async function deleteDevice(deviceId) {
    if (confirm('Bu cihazı silmek istediğinizden emin misiniz?')) {
        try {
            await deleteDeviceAPI(deviceId);
            showNotification('Cihaz başarıyla silindi!', 'success');
        } catch (error) {
            showNotification('Cihaz silinirken hata oluştu!', 'error');
        }
    }
}

function renderDeviceList() {
    const tbody = document.getElementById('deviceList');
    if (!tbody) {
        console.error('deviceList elementi bulunamadı');
        return;
    }
console.log(devices);
    tbody.innerHTML = devices.map(device => {
        // TimeOnly formatını düzenle (API'den "HH:MM:SS" geliyorsa sadece "HH:MM" al)
        const formatTime = (timeString) => {
            if (!timeString) return '--:--';
            // Eğer "HH:MM:SS" formatındaysa sadece "HH:MM" kısmını al
            return timeString.length > 5 ? timeString.substring(0, 5) : timeString;
        };

        return `
        <tr>
            <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex px-2 py-1 text-xs font-semibold rounded-full ${device.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}">
                    <i class="fas ${device.isActive ? 'fa-check-circle' : 'fa-times-circle'} mr-1"></i>
                    ${device.isActive ? 'Aktif' : 'Pasif'}
                </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${device.nebimOfficeCode}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${device.nebimStoreCode}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${device.pronetStoreCode || '-'}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 font-mono">${device.macAddress || '-'}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${formatTime(device.openingTime)} - ${formatTime(device.closingTime)}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                <button class="text-blue-600 hover:text-blue-900" onclick="editDevice(${device.id})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="text-red-600 hover:text-red-900" onclick="deleteDevice(${device.id})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `;
    }).join('');
}

// Dashboard Functions
function renderUpdateLogs() {
    const container = document.getElementById('updateLogsList');
    if (!container) {
        console.error('updateLogsList elementi bulunamadı');
        return;
    }

    if (updateLogs.length === 0) {
        container.innerHTML = `
            <div class="p-6 text-center text-gray-500">
                <i class="fas fa-inbox text-3xl mb-2"></i>
                <p>Henüz güncelleme kaydı yok</p>
            </div>
        `;
        return;
    }

    container.innerHTML = updateLogs.map(log => {
        const statusColor = log.IsSuccess ? 'text-green-600 bg-green-50' : 'text-red-600 bg-red-50';
        const statusIcon = log.IsSuccess ? 'fa-check-circle' : 'fa-times-circle';
        const statusText = log.IsSuccess ? 'Başarılı' : 'Hata';

        return `
            <div class="px-6 py-4 border-b border-gray-100 hover:bg-gray-50">
                <div class="flex items-center justify-between">
                    <div class="flex-1">
                        <div class="flex items-center justify-between mb-2">
                            <div class="flex items-center">
                                <h4 class="text-sm font-medium text-gray-900 mr-2">${log.DeviceStoreCode}</h4>
                                <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${statusColor}">
                                    <i class="fas ${statusIcon} mr-1"></i>
                                    ${statusText}
                                </span>
                            </div>
                            <button onclick="updateSingleStore('${log.DeviceStoreCode}')" class="text-blue-600 hover:text-blue-800 text-sm">
                                <i class="fas fa-sync mr-1"></i>
                                Şimdi Güncelle
                            </button>
                        </div>
                        <div class="text-xs text-gray-500 mb-1">${formatDateTime(log.RunTime)}</div>
                        <div class="flex items-center text-sm text-gray-600">
                            <span class="mr-4">
                                <i class="fas fa-sign-in-alt text-green-500 mr-1"></i>
                                Giriş: <strong>${log.EntryCount || 0}</strong>
                            </span>
                            <span class="mr-4">
                                <i class="fas fa-sign-out-alt text-red-500 mr-1"></i>
                                Çıkış: <strong>${log.ExitCount || 0}</strong>
                            </span>
                            ${log.ErrorMessage ? `<span class="text-xs text-red-600">${log.ErrorMessage}</span>` : ''}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

function formatDateTime(timestamp) {
    const date = new Date(timestamp);
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const logDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());

    if (logDate.getTime() === today.getTime()) {
        return `Bugün ${date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}`;
    } else {
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    }
}

function updateSingleStore(storeCode) {
    // Bu fonksiyon backend'de manuel sync tetiklemek için kullanılacak
    // Şimdilik sadece yeniden yükleyelim
    const button = event.target.closest('button');
    const originalText = button.innerHTML;

    button.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Güncelleniyor...';
    button.disabled = true;

    setTimeout(() => {
        loadDashboardStatus();
        button.innerHTML = originalText;
        button.disabled = false;
        showNotification('Güncelleme tamamlandı!', 'success');
    }, 2000);
}

function refreshAllStores() {
    // Tüm mağazaları güncelle
    const button = event.target;
    const originalText = button.innerHTML;

    button.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Güncelleniyor...';
    button.disabled = true;

    setTimeout(() => {
        loadDashboardStatus();
        button.innerHTML = originalText;
        button.disabled = false;
        showNotification('Tüm mağazalar güncellendi!', 'success');
    }, 3000);
}

function updateStats() {
    // Son güncelleme zamanını güncelle
    const lastUpdateEl = document.getElementById('lastUpdateTime');
    if (lastUpdateEl && updateLogs.length > 0) {
        const lastUpdate = updateLogs[0];
        lastUpdateEl.textContent = formatDateTime(lastUpdate.RunTime);
    }

    // Aktif mağaza sayısı
    const activeStoresEl = document.getElementById('activeStores');
    if (activeStoresEl) {
        const uniqueStores = [...new Set(updateLogs.map(log => log.DeviceStoreCode))];
        activeStoresEl.textContent = uniqueStores.length;
    }

    // Bugünkü toplam
    const todayLogs = updateLogs.filter(log => {
        const logDate = new Date(log.RunTime);
        const today = new Date();
        return logDate.toDateString() === today.toDateString();
    });

    const totalToday = todayLogs.reduce((sum, log) => sum + (log.EntryCount || 0), 0);
    const todayTotalEl = document.getElementById('todayTotal');
    if (todayTotalEl) {
        todayTotalEl.textContent = `${totalToday.toLocaleString()} kişi`;
    }
}

function initializeDateInputs() {
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    const startDateEl = document.getElementById('startDate');
    const endDateEl = document.getElementById('endDate');

    if (startDateEl) startDateEl.value = yesterday.toISOString().split('T')[0];
    if (endDateEl) endDateEl.value = today.toISOString().split('T')[0];
}

// Utility Functions
function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    if (!input) return;

    const icon = input.nextElementSibling.querySelector('i');

    if (input.type === 'password') {
        input.type = 'text';
        icon.className = 'fas fa-eye-slash text-gray-400 hover:text-gray-600';
    } else {
        input.type = 'password';
        icon.className = 'fas fa-eye text-gray-400 hover:text-gray-600';
    }
}

// Event Listeners
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM yüklendi, API entegrasyonu başlatılıyor');

    // Handle initial page load based on hash
    setTimeout(() => {
        handleHashChange();
    }, 100); // Kısa delay ile hash kontrolü

    // Listen for hash changes (back/forward buttons)
    window.addEventListener('hashchange', handleHashChange);

    initializeDateInputs();

    // Device form submission handler
    const deviceForm = document.getElementById('deviceForm');
    if (deviceForm) {
        deviceForm.addEventListener('submit', async function(e) {
            e.preventDefault();

            const formData = {
                active: document.getElementById('deviceActive').checked,
                nebimOfficeCode: document.getElementById('nebimOfficeCode').value,
                nebimStoreCode: document.getElementById('nebimStoreCode').value,
                pronetStoreCode: document.getElementById('pronetStoreCode').value,
                deviceMacAddress: document.getElementById('deviceMacAddress').value,
                openingTime: document.getElementById('openingTime').value,
                closingTime: document.getElementById('closingTime').value
            };

            const submitBtn = e.target.querySelector('button[type="submit"]');
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Kaydediliyor...';
            submitBtn.disabled = true;

            try {
                await saveDevice(formData);
                closeDeviceModal();
                showNotification('Cihaz başarıyla kaydedildi!', 'success');
            } catch (error) {
                showNotification('Cihaz kaydedilirken hata oluştu!', 'error');
            } finally {
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            }
        });
    }

    // MAC address formatting
    const macAddressInput = document.getElementById('deviceMacAddress');
    if (macAddressInput) {
        macAddressInput.addEventListener('input', function(e) {
            let value = e.target.value.replace(/[^0-9A-Fa-f]/g, '');
            if (value.length > 12) value = value.substr(0, 12);

            value = value.replace(/(.{2})/g, '$1:');
            if (value.endsWith(':')) value = value.slice(0, -1);

            e.target.value = value.toUpperCase();
        });
    }

    // Close modal on outside click
    const deviceModal = document.getElementById('deviceModal');
    if (deviceModal) {
        deviceModal.addEventListener('click', function(e) {
            if (e.target === this) {
                closeDeviceModal();
            }
        });
    }

    // Test buttons functionality
    const testPronetBtn = document.getElementById('testPronetBtn');
    if (testPronetBtn) {
        testPronetBtn.addEventListener('click', async function() {
            const originalText = this.innerHTML;
            this.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Pronet Test Ediliyor...';
            this.disabled = true;

            try {
                await testPronetConnection();
                this.innerHTML = '<i class="fas fa-check mr-2"></i>Pronet Bağlantısı Başarılı!';
                this.className = this.className.replace('text-gray-700 bg-white hover:bg-gray-50', 'text-green-700 bg-green-50 hover:bg-green-100 border-green-300');
                showNotification('Pronet bağlantısı başarılı!', 'success');

                setTimeout(() => {
                    this.innerHTML = originalText;
                    this.className = this.className.replace('text-green-700 bg-green-50 hover:bg-green-100 border-green-300', 'text-gray-700 bg-white hover:bg-gray-50');
                    this.disabled = false;
                }, 3000);
            } catch (error) {
                this.innerHTML = '<i class="fas fa-times mr-2"></i>Pronet Bağlantı Hatası!';
                this.className = this.className.replace('text-gray-700 bg-white hover:bg-gray-50', 'text-red-700 bg-red-50 hover:bg-red-100 border-red-300');
                showNotification('Pronet bağlantısı başarısız!', 'error');

                setTimeout(() => {
                    this.innerHTML = originalText;
                    this.className = this.className.replace('text-red-700 bg-red-50 hover:bg-red-100 border-red-300', 'text-gray-700 bg-white hover:bg-gray-50');
                    this.disabled = false;
                }, 3000);
            }
        });
    }

    const testNebimBtn = document.getElementById('testNebimBtn');
    if (testNebimBtn) {
        testNebimBtn.addEventListener('click', async function() {
            const originalText = this.innerHTML;
            this.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Nebim Test Ediliyor...';
            this.disabled = true;

            try {
                await testNebimConnection();
                this.innerHTML = '<i class="fas fa-check mr-2"></i>Nebim Bağlantısı Başarılı!';
                this.className = this.className.replace('text-gray-700 bg-white hover:bg-gray-50', 'text-green-700 bg-green-50 hover:bg-green-100 border-green-300');
                showNotification('Nebim bağlantısı başarılı!', 'success');

                setTimeout(() => {
                    this.innerHTML = originalText;
                    this.className = this.className.replace('text-green-700 bg-green-50 hover:bg-green-100 border-green-300', 'text-gray-700 bg-white hover:bg-gray-50');
                    this.disabled = false;
                }, 3000);
            } catch (error) {
                this.innerHTML = '<i class="fas fa-times mr-2"></i>Nebim Bağlantı Hatası!';
                this.className = this.className.replace('text-gray-700 bg-white hover:bg-gray-50', 'text-red-700 bg-red-50 hover:bg-red-100 border-red-300');
                showNotification('Nebim bağlantısı başarısız!', 'error');

                setTimeout(() => {
                    this.innerHTML = originalText;
                    this.className = this.className.replace('text-red-700 bg-red-50 hover:bg-red-100 border-red-300', 'text-gray-700 bg-white hover:bg-gray-50');
                    this.disabled = false;
                }, 3000);
            }
        });
    }

    const saveSettingsBtn = document.getElementById('saveSettingsBtn');
    if (saveSettingsBtn) {
        saveSettingsBtn.addEventListener('click', async function() {
            this.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Kaydediliyor...';
            this.disabled = true;

            try {
                await saveSettings();
                this.innerHTML = '<i class="fas fa-check mr-2"></i>Kaydedildi!';
                this.className = this.className.replace('bg-blue-600 hover:bg-blue-700', 'bg-green-600 hover:bg-green-700');
                showNotification('Ayarlar başarıyla kaydedildi!', 'success');

                setTimeout(() => {
                    this.innerHTML = '<i class="fas fa-save mr-2"></i>Ayarları Kaydet';
                    this.className = this.className.replace('bg-green-600 hover:bg-green-700', 'bg-blue-600 hover:bg-blue-700');
                    this.disabled = false;
                }, 2000);
            } catch (error) {
                this.innerHTML = '<i class="fas fa-times mr-2"></i>Kaydetme Hatası!';
                this.className = this.className.replace('bg-blue-600 hover:bg-blue-700', 'bg-red-600 hover:bg-red-700');
                showNotification('Ayarlar kaydedilirken hata oluştu!', 'error');

                setTimeout(() => {
                    this.innerHTML = '<i class="fas fa-save mr-2"></i>Ayarları Kaydet';
                    this.className = this.className.replace('bg-red-600 hover:bg-red-700', 'bg-blue-600 hover:bg-blue-700');
                    this.disabled = false;
                }, 3000);
            }
        });
    }

    // Date range form handler
    const dateRangeForm = document.getElementById('dateRangeForm');
    if (dateRangeForm) {
        dateRangeForm.addEventListener('submit', function(e) {
            e.preventDefault();

            const storeCode = document.getElementById('storeSelect').value;
            const startDate = document.getElementById('startDate').value;
            const endDate = document.getElementById('endDate').value;

            const progressDiv = document.getElementById('updateProgress');
            const progressBar = document.getElementById('progressBar');
            const progressText = document.getElementById('progressText');
            const submitBtn = e.target.querySelector('button[type="submit"]');

            // Show progress
            progressDiv.classList.remove('hidden');
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Güncelleniyor...';

            let progress = 0;
            const interval = setInterval(() => {
                progress += Math.random() * 20;
                if (progress > 95) progress = 95;

                progressBar.style.width = progress + '%';
                progressText.textContent = Math.round(progress) + '%';
            }, 200);

            setTimeout(() => {
                clearInterval(interval);
                progressBar.style.width = '100%';
                progressText.textContent = '100%';

                // Reload dashboard data
                loadDashboardStatus();
                showNotification(`${storeCode} mağazası için tarih aralığı güncellendi!`, 'success');

                setTimeout(() => {
                    progressDiv.classList.add('hidden');
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = '<i class="fas fa-calendar-alt mr-2"></i>Tarih Aralığını Güncelle';
                    progressBar.style.width = '0%';
                    progressText.textContent = '0%';
                }, 1000);
            }, 3000);
        });
    }
});