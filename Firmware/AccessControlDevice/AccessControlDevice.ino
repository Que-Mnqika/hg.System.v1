#include <WiFi.h>
#include <esp_wifi.h>
#include <esp_efuse.h>
#include <esp_mac.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <SPI.h>
#include <Adafruit_PN532.h>
#include <EEPROM.h>
#include <time.h>
#include <nvs_flash.h>
#include <Preferences.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

// ============================================================================
//  DEBUG CONFIGURATION
// ============================================================================
bool DEBUG_API = true;  // Set to false to disable API debug output (can be toggled at runtime)

// ============================================================================
//  PIN DEFINITIONS
// ============================================================================
#define PN532_SCK   18
#define PN532_MOSI  23
#define PN532_MISO  19
#define PN532_SS    5
#define LED_RED     27
#define LED_GREEN   25
#define LED_BLUE    26
#define BUZZER_PIN  14
#define OLED_SDA    22
#define OLED_SCL    21

// ============================================================================
//  OLED DISPLAY
// ============================================================================
#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_ADDR    0x3C
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, -1);

// ============================================================================
//  NETWORK & SERVER CONFIGURATION
// ============================================================================
// Primary Network (HGTS HQ)
const char* primary_ssid       = "Galaxy A16 5G e6ba";
const char* primary_password   = "hgtsDevs";

// Fallback Network (Phone Hotspot) - CHANGE THESE
const char* fallback_ssid      = "YourPhoneHotspot";
const char* fallback_password  = "HotspotPassword";

// Static IP Configuration for Primary Network
bool use_static_ip = true;
IPAddress static_ip(192, 168, 11, 100);
IPAddress gateway(192, 168, 11, 1);
IPAddress subnet(255, 255, 255, 0);
IPAddress dns(8, 8, 8, 8);

const char* serverHost = "10.27.26.125";
const int   serverPort = 5277;
const char* deviceId   = "2501c157-a6f2-4080-8e1b-28c936d203c2";

HTTPClient http;
Preferences prefs;

// ============================================================================
//  WEATHER API
// ============================================================================
const char* weatherApiKey = "2c100472014d4f5b9b173247262705";
const char* weatherCity   = "Cape Town";
const char* weatherCountry= "ZA";

// ============================================================================
//  HCE / AID CONFIGURATION
// ============================================================================
uint8_t SELECT_AID[]  = {0x00,0xA4,0x04,0x00,0x05,0xF0,0x01,0x02,0x03,0x04,0x00};
const int SELECT_AID_LEN = 11;

// ============================================================================
//  STORAGE LIMITS
// ============================================================================
#define MAX_OFFLINE_TAPS      30
#define MAX_BOARDED_STUDENTS  30
#define MAX_MANIFEST_TOKENS   80
#define MAX_TRIP_STOPS        10
#define EEPROM_SIZE           4096
#define POST_TAP_DELAY_MS     500
#define MAGIC_NUMBER          0xABCD1249
#define INACTIVITY_TIMEOUT_MS 300000

// ============================================================================
//  TIMING
// ============================================================================
#define CONFIG_REFRESH_INTERVAL_MS  60000
#define NTP_SYNC_INTERVAL_MS        7200000
#define SYNC_RETRY_INTERVAL_MS      60000
#define HEARTBEAT_INTERVAL_MS       120000
#define WEATHER_REFRESH_INTERVAL_MS 900000
#define PN532_RESET_INTERVAL_MS     30000

// ============================================================================
//  DATA STRUCTURES
// ============================================================================
struct OfflineTap {
  char  token[32];
  char  tripId[37];
  char  rawUid[20];
  char  deviceType[8];
  char  resultCode[16];
  char  message[60];
  char  readableTime[20];
  unsigned long timestamp;
  unsigned long clientTimestamp;
  bool  accessGranted;
  bool  routeMismatch;
  bool  synced;
};

struct BoardedStudent {
  char studentId[37];
  char token[32];
  char residenceId[37];
  char tripId[37];
  int  seatNumber;
  unsigned long boardTime;
};

struct TripConfig {
  char  tripId[37];
  char  routeId[37];
  char  routeName[48];
  char  residenceId[37];
  char  residenceName[48];
  char  stopResidenceIds[MAX_TRIP_STOPS][37];
  int   vehicleId;
  int   stopCount;
  int   durationMinutes;
  int   vehicleCapacity;
  int   currentPassengers;
  int   nextSeatNumber;
  unsigned long startTime;
  unsigned long endTime;
  bool  isActive;
};

struct ManifestToken {
  char token[32];
  char studentId[37];
  char residenceId[37];
  char fullName[48];
  char type[6];
};

// ============================================================================
//  GLOBALS
// ============================================================================
bool wifiConnected    = false;
bool apiReachable     = false;
bool nfcReady         = false;
bool serialMenuActive = false;
bool hasActiveTrip    = false;
bool timeSynced       = false;
bool manifestLoaded   = false;
bool displaySleepMode = false;
int  pn532FailCount   = 0;
unsigned long lastActivityTime = 0;
unsigned long lastWeatherUpdate = 0;
unsigned long lastPn532Reset = 0;
unsigned long lastWifiCheck     = 0;
unsigned long lastTapTime       = 0;
unsigned long lastConfigRefresh = 0;
unsigned long lastSyncAttempt   = 0;
unsigned long lastNtpAttempt    = 0;
unsigned long lastHeartbeat     = 0;
unsigned long bootTime          = 0;
unsigned long bootEpoch         = 0;

OfflineTap     offlineTaps[MAX_OFFLINE_TAPS];
int            offlineTapCount = 0;
TripConfig     currentTrip;
BoardedStudent boardedStudents[MAX_BOARDED_STUDENTS];
int            boardedCount = 0;
ManifestToken  manifest[MAX_MANIFEST_TOKENS];
int            manifestCount = 0;

// Weather cache
String currentWeather = "";
float currentTemp = 0;

Adafruit_PN532 nfc(PN532_SCK, PN532_MISO, PN532_MOSI, PN532_SS);

// ============================================================================
//  DEBUG HELPER FUNCTION
// ============================================================================
void debugPrintApi(String url, String method, String body, int httpCode, String response = "") {
  if (!DEBUG_API) return;
  
  Serial.println("\n╔══════════════════════════════════════════════════════════════╗");
  Serial.println("║                    🔍 API DEBUG INFO                        ║");
  Serial.println("╠══════════════════════════════════════════════════════════════╣");
  Serial.printf("║ URL:      %s\n", url.c_str());
  Serial.printf("║ Method:   %s\n", method.c_str());
  Serial.printf("║ Body:     %s\n", body.c_str());
  Serial.printf("║ HTTP Code: %d\n", httpCode);
  if (response.length() > 0) {
    String shortResponse = response.substring(0, 300);
    Serial.printf("║ Response: %s\n", shortResponse.c_str());
    if (response.length() > 300) {
      Serial.println("║ ... (response truncated)");
    }
  }
  Serial.println("╚══════════════════════════════════════════════════════════════╝\n");
}

// ============================================================================
//  TIME HELPERS
// ============================================================================
unsigned long getUtcEpoch() {
  struct tm timeinfo;
  if (timeSynced && getLocalTime(&timeinfo, 0)) {
    return mktime(&timeinfo);
  }
  return bootEpoch + (millis() / 1000);
}

String getReadableTime() {
  struct tm timeinfo;
  if (timeSynced && getLocalTime(&timeinfo, 0)) {
    char buf[25];
    strftime(buf, sizeof(buf), "%Y-%m-%d %H:%M:%S", &timeinfo);
    return String(buf);
  }
  return String(millis() / 1000) + "s";
}

String getFormattedTime() {
  struct tm timeinfo;
  if (timeSynced && getLocalTime(&timeinfo, 0)) {
    char buf[9];
    strftime(buf, sizeof(buf), "%H:%M:%S", &timeinfo);
    return String(buf);
  }
  return String(millis() / 1000) + "s";
}

String getFormattedDate() {
  struct tm timeinfo;
  if (timeSynced && getLocalTime(&timeinfo, 0)) {
    char buf[12];
    strftime(buf, sizeof(buf), "%d/%m/%Y", &timeinfo);
    return String(buf);
  }
  return "No sync";
}

void syncTimeInBackground() {
  if (!wifiConnected) return;
  unsigned long now = millis();
  if (!timeSynced && (now - lastNtpAttempt > 5000)) {
    lastNtpAttempt = now;
    configTime(7200, 0, "pool.ntp.org", "time.nist.gov");
    struct tm t;
    if (getLocalTime(&t, 2000)) {
      timeSynced = true;
      bootEpoch = getUtcEpoch() - (millis() / 1000);
      Serial.println("🕐 Time synced");
    }
  } else if (timeSynced && (now - lastNtpAttempt > NTP_SYNC_INTERVAL_MS)) {
    lastNtpAttempt = now;
    configTime(7200, 0, "pool.ntp.org", "time.nist.gov");
    struct tm t; getLocalTime(&t, 500);
  }
}

// ============================================================================
//  WEATHER FETCH
// ============================================================================
void fetchWeather() {
  if (!wifiConnected || !apiReachable) {
    currentWeather = "";
    return;
  }
  
  String url = "http://api.openweathermap.org/data/2.5/weather?q=" +
               String(weatherCity) + "," + String(weatherCountry) +
               "&units=metric&appid=" + String(weatherApiKey);
  
  http.begin(url);
  http.setTimeout(3000);
  int code = http.GET();
  
  if (code == 200) {
    String response = http.getString();
    StaticJsonDocument<512> doc;
    if (deserializeJson(doc, response) == DeserializationError::Ok) {
      currentWeather = doc["weather"][0]["description"] | "";
      currentTemp = doc["main"]["temp"] | 0;
      if (currentWeather.length() > 12) currentWeather = currentWeather.substring(0, 12);
    }
  }
  http.end();
  lastWeatherUpdate = millis();
}

// ============================================================================
//  OLED DISPLAY
// ============================================================================
void initDisplay() {
  Wire.begin(OLED_SDA, OLED_SCL);
  if (!display.begin(SSD1306_SWITCHCAPVCC, OLED_ADDR)) {
    Serial.println("❌ OLED failed");
  } else {
    display.clearDisplay();
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.display();
  }
}

void updateDisplay() {
  if (!display.begin(SSD1306_SWITCHCAPVCC, OLED_ADDR)) return;
  
  display.clearDisplay();
  display.setTextSize(1);
  display.setCursor(0, 0);
  
  if (hasActiveTrip && isTripActive()) {
    display.setTextSize(2);
    display.setCursor(0, 0);
    display.printf("%d/%d", currentTrip.currentPassengers, currentTrip.vehicleCapacity);
    
    display.setTextSize(1);
    display.setCursor(0, 18);
    display.printf("Next Seat: %d", currentTrip.nextSeatNumber);
    
    display.setCursor(0, 28);
    display.printf("%s", currentTrip.routeName);
    
    unsigned long remaining = (currentTrip.endTime - millis()) / 60000;
    display.setCursor(0, 38);
    display.printf("Left: %lum", remaining);
    
    display.setCursor(0, 50);
    display.printf("%s", getFormattedTime().c_str());
    
  } else if (displaySleepMode) {
    display.setTextSize(2);
    display.setCursor(0, 0);
    display.printf("%s", getFormattedTime().c_str());
    
    display.setTextSize(1);
    display.setCursor(0, 20);
    display.printf("%s", getFormattedDate().c_str());
    
    if (currentWeather.length() > 0 && currentTemp != 0) {
      display.setCursor(0, 35);
      display.printf("%.0fC %s", currentTemp, currentWeather.c_str());
    } else if (currentWeather.length() > 0) {
      display.setCursor(0, 35);
      display.printf("%s", currentWeather.c_str());
    }
    
    display.setCursor(0, 50);
    display.printf("WiFi:%s", wifiConnected ? "ON" : "OFF");
    
  } else {
    display.setTextSize(2);
    display.setCursor(0, 0);
    display.printf("HGTS");
    
    display.setTextSize(1);
    display.setCursor(0, 20);
    display.printf("Tap to board");
    
    display.setCursor(0, 35);
    display.printf("WiFi:%s", wifiConnected ? "OK" : "OFF");
    
    display.setCursor(0, 50);
    display.printf("%s", getFormattedTime().c_str());
  }
  
  display.display();
}

// ============================================================================
//  LED & BUZZER
// ============================================================================
void allLedsOff() {
  digitalWrite(LED_RED, LOW); digitalWrite(LED_GREEN, LOW); digitalWrite(LED_BLUE, LOW);
}
void ledGreen()  { allLedsOff(); digitalWrite(LED_GREEN, HIGH); }
void ledRed()    { allLedsOff(); digitalWrite(LED_RED, HIGH); }
void ledBlue()   { allLedsOff(); digitalWrite(LED_BLUE, HIGH); }
void ledYellow() { allLedsOff(); digitalWrite(LED_RED, HIGH); digitalWrite(LED_GREEN, HIGH); }

void buzzPattern(int pattern) {
  switch(pattern) {
    case 1:
      digitalWrite(BUZZER_PIN, HIGH); delay(120);
      digitalWrite(BUZZER_PIN, LOW);  delay(100);
      digitalWrite(BUZZER_PIN, HIGH); delay(120);
      digitalWrite(BUZZER_PIN, LOW);
      break;
    case 2:
      for (int i = 0; i < 3; i++) {
        digitalWrite(BUZZER_PIN, HIGH); delay(80);
        digitalWrite(BUZZER_PIN, LOW);  delay(80);
      }
      break;
    case 3:
      digitalWrite(BUZZER_PIN, HIGH); delay(600);
      digitalWrite(BUZZER_PIN, LOW);
      break;
    case 4:
      for (int i = 0; i < 4; i++) {
        digitalWrite(BUZZER_PIN, HIGH); delay(100);
        digitalWrite(BUZZER_PIN, LOW);  delay(100);
      }
      break;
  }
}

// ============================================================================
//  FEEDBACK
// ============================================================================
void feedbackGranted(String name = "", String studentId = "", int seatNumber = 0) {
  Serial.printf("✅ GRANTED: %s | Seat: %d\n", name.c_str(), seatNumber);
  ledGreen();
  buzzPattern(1);
  delay(300); allLedsOff();
  lastActivityTime = millis();
  updateDisplay();
}

void feedbackMismatch(String studentRes = "", String tripRes = "") {
  Serial.printf("🟡 MISMATCH: Wrong bus\n");
  ledYellow();
  buzzPattern(2);
  delay(300); allLedsOff();
  lastActivityTime = millis();
  updateDisplay();
}

void feedbackDenied(String reason = "") {
  Serial.printf("❌ DENIED: %s\n", reason.c_str());
  ledRed();
  buzzPattern(3);
  delay(300); allLedsOff();
  lastActivityTime = millis();
  updateDisplay();
}

void feedbackOverload(String name = "") {
  Serial.printf("🚫 FULL: Bus at capacity\n");
  ledRed();
  for (int i = 0; i < 3; i++) {
    buzzPattern(4);
    delay(200);
  }
  delay(300); allLedsOff();
  lastActivityTime = millis();
  updateDisplay();
}

void feedbackOfflineStorage(String resultCode = "PENDING") {
  Serial.printf("💾 OFFLINE: %s queued\n", resultCode.c_str());
  ledBlue(); delay(100); allLedsOff(); delay(100);
  ledBlue(); delay(100); allLedsOff();
  lastActivityTime = millis();
}

void feedbackDuplicate() {
  Serial.printf("🔁 DUPLICATE: Already boarded\n");
  ledRed();
  buzzPattern(2);
  delay(200); allLedsOff();
  lastActivityTime = millis();
}

void feedbackNoActiveTrip() {
  Serial.println("⚠️ No active trip");
  ledBlue(); delay(150); allLedsOff(); delay(150);
  ledBlue(); delay(150); allLedsOff(); delay(150);
  ledBlue(); delay(150); allLedsOff();
}

void feedbackStartup() {
  ledGreen(); delay(100); allLedsOff();
  ledYellow(); delay(100); allLedsOff();
  ledRed(); delay(100); allLedsOff();
  buzzPattern(1);
}

// ============================================================================
//  CAPACITY MANAGEMENT
// ============================================================================
bool isVehicleFull() {
  return boardedCount >= currentTrip.vehicleCapacity;
}

int getNextSeatNumber() {
  return currentTrip.nextSeatNumber;
}

void incrementSeatNumber() {
  currentTrip.nextSeatNumber++;
  if (currentTrip.nextSeatNumber > currentTrip.vehicleCapacity) {
    currentTrip.nextSeatNumber = currentTrip.vehicleCapacity;
  }
}

void updateVehicleCapacityFromBackend() {
  if (!wifiConnected || !apiReachable) return;
  
  String url = "http://" + String(serverHost) + ":" + String(serverPort) +
               "/api/vehicles/" + String(currentTrip.vehicleId) + "/capacity";
  http.begin(url);
  http.setTimeout(3000);
  int code = http.GET();
  
  if (code == 200) {
    String response = http.getString();
    StaticJsonDocument<256> doc;
    if (deserializeJson(doc, response) == DeserializationError::Ok) {
      currentTrip.vehicleCapacity = doc["capacity"] | 50;
      currentTrip.currentPassengers = doc["currentPassengers"] | boardedCount;
    }
  }
  http.end();
}

// ============================================================================
//  PERSISTENT STORAGE
// ============================================================================
void saveBoardedStudentsToNVS() {
  prefs.begin("boarded", false);
  prefs.putInt("count", boardedCount);
  prefs.putInt("nextSeat", currentTrip.nextSeatNumber);
  prefs.putString("tripId", currentTrip.tripId);
  for (int i = 0; i < boardedCount; i++) {
    String key = "s" + String(i);
    String entry = String(boardedStudents[i].studentId) + "|" +
                   String(boardedStudents[i].token) + "|" +
                   String(boardedStudents[i].residenceId) + "|" +
                   String(boardedStudents[i].seatNumber) + "|" +
                   String(boardedStudents[i].boardTime);
    prefs.putString(key.c_str(), entry);
  }
  prefs.end();
}

void loadBoardedStudentsFromNVS() {
  prefs.begin("boarded", true);
  String savedTripId = prefs.getString("tripId", "");
  int count = prefs.getInt("count", 0);
  int nextSeat = prefs.getInt("nextSeat", 1);
  
  if (count > 0 && savedTripId == String(currentTrip.tripId)) {
    boardedCount = 0;
    currentTrip.nextSeatNumber = nextSeat;
    for (int i = 0; i < count && i < MAX_BOARDED_STUDENTS; i++) {
      String key = "s" + String(i);
      String entry = prefs.getString(key.c_str(), "");
      if (entry.length() == 0) continue;
      
      int p1 = entry.indexOf('|');
      int p2 = entry.indexOf('|', p1 + 1);
      int p3 = entry.indexOf('|', p2 + 1);
      int p4 = entry.indexOf('|', p3 + 1);
      
      entry.substring(0, p1).toCharArray(boardedStudents[i].studentId, 37);
      entry.substring(p1+1, p2).toCharArray(boardedStudents[i].token, 32);
      entry.substring(p2+1, p3).toCharArray(boardedStudents[i].residenceId, 37);
      boardedStudents[i].seatNumber = entry.substring(p3+1, p4).toInt();
      boardedStudents[i].boardTime = entry.substring(p4+1).toInt();
      strcpy(boardedStudents[i].tripId, currentTrip.tripId);
      boardedCount++;
    }
    currentTrip.currentPassengers = boardedCount;
  }
  prefs.end();
}

void clearBoardedStudents() {
  boardedCount = 0;
  currentTrip.currentPassengers = 0;
  currentTrip.nextSeatNumber = 1;
  memset(boardedStudents, 0, sizeof(boardedStudents));
  prefs.begin("boarded", false);
  prefs.clear();
  prefs.end();
}

// ============================================================================
//  MANIFEST
// ============================================================================
bool downloadManifest(String tripId) {
  if (!wifiConnected || !apiReachable) return false;

  String url = "http://" + String(serverHost) + ":" + String(serverPort) +
               "/api/manifest/trip/" + tripId;
  http.begin(url);
  http.setTimeout(8000);
  int code = http.GET();

  if (code != 200) {
    http.end();
    return false;
  }

  String response = http.getString();
  http.end();

  StaticJsonDocument<4096> doc;
  if (deserializeJson(doc, response) != DeserializationError::Ok) {
    return false;
  }

  manifestCount = 0;
  currentTrip.stopCount = 0;
  
  JsonArray stops = doc["stops"];
  for (JsonObject s : stops) {
    if (currentTrip.stopCount >= MAX_TRIP_STOPS) break;
    String rid = s["residenceId"] | "";
    rid.toCharArray(currentTrip.stopResidenceIds[currentTrip.stopCount], 37);
    currentTrip.stopCount++;
  }
  
  JsonArray tokens = doc["tokens"];
  for (JsonObject t : tokens) {
    if (manifestCount >= MAX_MANIFEST_TOKENS) break;
    String tok = t["token"]       | "";
    String sid = t["studentId"]   | "";
    String rid = t["residenceId"] | "";
    String nam = t["fullName"]    | "";
    String typ = t["type"]        | "CARD";

    tok.toCharArray(manifest[manifestCount].token,       32);
    sid.toCharArray(manifest[manifestCount].studentId,   37);
    rid.toCharArray(manifest[manifestCount].residenceId, 37);
    nam.toCharArray(manifest[manifestCount].fullName,    48);
    typ.toCharArray(manifest[manifestCount].type,        6);
    manifestCount++;
  }

  manifestLoaded = true;
  Serial.printf("📋 Manifest: %d tokens, %d stops\n", manifestCount, currentTrip.stopCount);

  prefs.begin("manifest", false);
  prefs.putInt("count", manifestCount);
  prefs.putInt("stopCount", currentTrip.stopCount);
  for (int i = 0; i < manifestCount; i++) {
    String key = "t" + String(i);
    String entry = String(manifest[i].token) + "|" +
                   String(manifest[i].studentId) + "|" +
                   String(manifest[i].residenceId) + "|" +
                   String(manifest[i].fullName) + "|" +
                   String(manifest[i].type);
    prefs.putString(key.c_str(), entry);
  }
  for (int i = 0; i < currentTrip.stopCount; i++) {
    String key = "stop" + String(i);
    prefs.putString(key.c_str(), String(currentTrip.stopResidenceIds[i]));
  }
  prefs.putString("tripId", tripId);
  prefs.end();

  return true;
}

void loadManifestFromNVS() {
  prefs.begin("manifest", true);
  String savedTripId = prefs.getString("tripId", "");
  int count = prefs.getInt("count", 0);
  int stops = prefs.getInt("stopCount", 0);

  if (count > 0 && savedTripId == String(currentTrip.tripId)) {
    manifestCount = 0;
    for (int i = 0; i < count && i < MAX_MANIFEST_TOKENS; i++) {
      String key = "t" + String(i);
      String entry = prefs.getString(key.c_str(), "");
      if (entry.length() == 0) continue;

      int p1 = entry.indexOf('|');
      int p2 = entry.indexOf('|', p1 + 1);
      int p3 = entry.indexOf('|', p2 + 1);
      int p4 = entry.indexOf('|', p3 + 1);

      entry.substring(0, p1).toCharArray(manifest[i].token,       32);
      entry.substring(p1+1, p2).toCharArray(manifest[i].studentId,   37);
      entry.substring(p2+1, p3).toCharArray(manifest[i].residenceId, 37);
      entry.substring(p3+1, p4).toCharArray(manifest[i].fullName,    48);
      entry.substring(p4+1).toCharArray(manifest[i].type,        6);
      manifestCount++;
    }
    
    currentTrip.stopCount = 0;
    for (int i = 0; i < stops && i < MAX_TRIP_STOPS; i++) {
      String key = "stop" + String(i);
      String rid = prefs.getString(key.c_str(), "");
      if (rid.length() > 0) {
        rid.toCharArray(currentTrip.stopResidenceIds[currentTrip.stopCount], 37);
        currentTrip.stopCount++;
      }
    }
    
    manifestLoaded = manifestCount > 0;
  }
  prefs.end();
}

// ============================================================================
//  BOARDED STUDENT MANAGEMENT
// ============================================================================
bool isStudentBoarded(String studentId) {
  for (int i = 0; i < boardedCount; i++)
    if (String(boardedStudents[i].studentId) == studentId) return true;
  return false;
}

bool isTokenBoarded(String token) {
  for (int i = 0; i < boardedCount; i++)
    if (String(boardedStudents[i].token) == token) return true;
  return false;
}

bool addBoardedStudent(String studentId, String token, String residenceId) {
  if (isStudentBoarded(studentId)) return false;
  if (isVehicleFull()) return false;
  
  if (boardedCount < MAX_BOARDED_STUDENTS) {
    int seatNumber = getNextSeatNumber();
    
    studentId.toCharArray(boardedStudents[boardedCount].studentId,   37);
    token.toCharArray(boardedStudents[boardedCount].token,           32);
    residenceId.toCharArray(boardedStudents[boardedCount].residenceId, 37);
    boardedStudents[boardedCount].seatNumber = seatNumber;
    boardedStudents[boardedCount].boardTime = millis();
    strcpy(boardedStudents[boardedCount].tripId, currentTrip.tripId);
    boardedCount++;
    currentTrip.currentPassengers = boardedCount;
    incrementSeatNumber();
    saveBoardedStudentsToNVS();
    
    Serial.printf("   👥 Boarded: %s | Seat: %d | Total: %d/%d\n", 
                  studentId.c_str(), seatNumber, boardedCount, currentTrip.vehicleCapacity);
    updateDisplay();
    return true;
  }
  return false;
}

// ============================================================================
//  OFFLINE TAP STORAGE
// ============================================================================
void saveOfflineTaps() {
  int addr = 0;
  uint32_t magic = MAGIC_NUMBER;
  EEPROM.put(addr, magic);       addr += sizeof(magic);
  EEPROM.put(addr, offlineTapCount); addr += sizeof(offlineTapCount);
  for (int i = 0; i < offlineTapCount; i++) {
    EEPROM.put(addr, offlineTaps[i]);
    addr += sizeof(OfflineTap);
  }
  EEPROM.commit();
}

void loadOfflineTaps() {
  uint32_t magic; EEPROM.get(0, magic);
  if (magic != MAGIC_NUMBER) { offlineTapCount = 0; saveOfflineTaps(); return; }
  int addr = sizeof(magic);
  EEPROM.get(addr, offlineTapCount); addr += sizeof(offlineTapCount);
  for (int i = 0; i < offlineTapCount && i < MAX_OFFLINE_TAPS; i++) {
    EEPROM.get(addr, offlineTaps[i]);
    addr += sizeof(OfflineTap);
  }
}

void storeOfflineTap(String token, String rawUid, String deviceType,
                     String resultCode, String message,
                     bool accessGranted, bool routeMismatch) {
  if (offlineTapCount >= MAX_OFFLINE_TAPS) return;
  
  OfflineTap tap;
  token.toCharArray(tap.token,           sizeof(tap.token));
  rawUid.toCharArray(tap.rawUid,         sizeof(tap.rawUid));
  deviceType.toCharArray(tap.deviceType, sizeof(tap.deviceType));
  resultCode.toCharArray(tap.resultCode, sizeof(tap.resultCode));
  message.toCharArray(tap.message,       sizeof(tap.message));
  strcpy(tap.tripId, currentTrip.tripId);
  tap.timestamp       = millis();
  tap.clientTimestamp = getUtcEpoch();
  tap.accessGranted   = accessGranted;
  tap.routeMismatch   = routeMismatch;
  tap.synced          = false;
  getReadableTime().toCharArray(tap.readableTime, sizeof(tap.readableTime));

  offlineTaps[offlineTapCount++] = tap;
  saveOfflineTaps();
}

void syncOfflineTaps() {
  if (offlineTapCount == 0 || !wifiConnected || !apiReachable) return;

  String url = "http://" + String(serverHost) + ":" + String(serverPort) +
               "/api/manifest/sync";

  StaticJsonDocument<4096> doc;
  JsonArray taps = doc.createNestedArray("taps");

  for (int i = 0; i < offlineTapCount; i++) {
    if (offlineTaps[i].synced) continue;
    JsonObject t = taps.createNestedObject();
    t["token"]         = offlineTaps[i].token;
    t["rawUid"]        = offlineTaps[i].rawUid;
    t["deviceId"]      = deviceId;
    t["tripId"]        = offlineTaps[i].tripId;
    t["accessGranted"] = offlineTaps[i].accessGranted;
    t["routeMismatch"] = offlineTaps[i].routeMismatch;
    t["resultCode"]    = offlineTaps[i].resultCode;
    t["message"]       = offlineTaps[i].message;
    t["timestamp"]     = offlineTaps[i].clientTimestamp;
    t["readableTime"]  = offlineTaps[i].readableTime;
  }

  String body;
  serializeJson(doc, body);

  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  http.setTimeout(10000);
  int code = http.POST(body);

  if (code == 200) {
    offlineTapCount = 0;
    memset(offlineTaps, 0, sizeof(offlineTaps));
    saveOfflineTaps();
  }
  http.end();
}

// ============================================================================
//  TRIP MANAGEMENT
// ============================================================================
bool isResidenceInTripStops(String residenceId) {
  for (int i = 0; i < currentTrip.stopCount; i++) {
    if (String(currentTrip.stopResidenceIds[i]) == residenceId) return true;
  }
  return false;
}

void startNewTrip(String tripId, String routeId, String routeName,
                  String residenceId, String residenceName, int durationMinutes,
                  int vehicleId) {
  bool isNewTrip = (String(currentTrip.tripId) != tripId);
  
  if (isNewTrip) {
    clearBoardedStudents();
    manifestLoaded = false;
    manifestCount  = 0;
    currentTrip.stopCount = 0;
    currentTrip.nextSeatNumber = 1;
  }

  tripId.toCharArray(currentTrip.tripId,           37);
  routeId.toCharArray(currentTrip.routeId,         37);
  routeName.toCharArray(currentTrip.routeName,     48);
  residenceId.toCharArray(currentTrip.residenceId, 37);
  residenceName.toCharArray(currentTrip.residenceName, 48);
  currentTrip.durationMinutes = durationMinutes;
  currentTrip.vehicleId = vehicleId;
  currentTrip.startTime = millis();
  currentTrip.endTime   = currentTrip.startTime + ((unsigned long)durationMinutes * 60000UL);
  currentTrip.isActive  = true;
  hasActiveTrip = true;

  updateVehicleCapacityFromBackend();

  if (isNewTrip) {
    if (wifiConnected && apiReachable) {
      downloadManifest(tripId);
    } else {
      loadManifestFromNVS();
    }
    loadBoardedStudentsFromNVS();
  }
  updateDisplay();
}

bool isTripActive() {
  return hasActiveTrip && millis() < currentTrip.endTime;
}

void endTrip() {
  if (hasActiveTrip) {
    clearBoardedStudents();
    manifestLoaded = false;
    manifestCount  = 0;
    hasActiveTrip  = false;
    currentTrip.stopCount = 0;
    updateDisplay();
  }
}

void checkTripExpiry() {
  if (hasActiveTrip && !isTripActive()) endTrip();
}

// ============================================================================
//  HEARTBEAT & TELEMETRY
// ============================================================================
void sendHeartbeat() {
  if (!wifiConnected || !apiReachable) return;
  
  String url = "http://" + String(serverHost) + ":" + String(serverPort) +
               "/api/devices/" + String(deviceId) + "/telemetry";
  
  StaticJsonDocument<256> doc;
  doc["firmwareVersion"] = "2.1.0";
  doc["rssi"] = WiFi.RSSI();
  doc["currentPassengers"] = boardedCount;
  doc["vehicleCapacity"] = currentTrip.vehicleCapacity;
  
  String body;
  serializeJson(doc, body);
  
  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  http.setTimeout(3000);
  int code = http.POST(body);
  http.end();
  
  if (DEBUG_API && code == 200) {
    Serial.println("❤️ Heartbeat sent");
  }
}

// ============================================================================
//  FETCH ACTIVE TRIP
// ============================================================================
bool fetchActiveTripFromBackend() {
  if (!wifiConnected || !apiReachable) return false;

  String url = "http://" + String(serverHost) + ":" + String(serverPort) +
               "/api/devices/" + String(deviceId) + "/active-trip";
  http.begin(url);
  http.setTimeout(5000);
  int code = http.GET();

  if (code != 200) { http.end(); return false; }

  String response = http.getString();
  http.end();

  StaticJsonDocument<1024> doc;
  if (deserializeJson(doc, response)) return false;

  bool hasTrip = doc["hasActiveTrip"] | false;
  if (!hasTrip) {
    if (hasActiveTrip) endTrip();
    return true;
  }

  String tripId        = doc["tripId"]        | "";
  String routeId       = doc["routeId"]       | "";
  String routeName     = doc["routeName"]     | "";
  String residenceId   = doc["residenceId"]   | "";
  String residenceName = doc["residenceName"] | "";
  int durationMinutes  = doc["durationMinutes"] | 30;
  int vehicleId        = doc["vehicleId"]     | 0;
  int vehicleCapacity  = doc["vehicleCapacity"] | 50;

  if (tripId != String(currentTrip.tripId) || !hasActiveTrip) {
    startNewTrip(tripId, routeId, routeName,
                 residenceId, residenceName, durationMinutes, vehicleId);
    currentTrip.vehicleCapacity = vehicleCapacity;
  }

  return true;
}

// ============================================================================
//  OFFLINE VALIDATION
// ============================================================================
String validateOffline(String token, String &outStudentId,
                       String &outName, String &outResidenceId) {
  if (isTokenBoarded(token)) return "ALREADY_BOARDED";
  if (isVehicleFull()) return "OVERLOAD";

  for (int i = 0; i < manifestCount; i++) {
    if (String(manifest[i].token) == token) {
      outStudentId  = String(manifest[i].studentId);
      outName       = String(manifest[i].fullName);
      outResidenceId= String(manifest[i].residenceId);
      
      if (isStudentBoarded(outStudentId)) return "ALREADY_BOARDED";
      if (isResidenceInTripStops(outResidenceId)) return "SUCCESS";
      return "ROUTE_MISMATCH";
    }
  }
  return "UNKNOWN_CREDENTIAL";
}

// ============================================================================
//  MAIN TAP HANDLER
// ============================================================================
void sendToBackend(String token, String type, String rawUid) {
  Serial.printf("🔍 NFC Detected - UID: %s | Type: %s\n", rawUid.c_str(), type.c_str());
  
  if (!hasActiveTrip || !isTripActive()) {
    feedbackNoActiveTrip();
    return;
  }

  // ONLINE PATH
  if (wifiConnected && apiReachable) {
    String url = "http://" + String(serverHost) + ":" + String(serverPort) +
                 "/api/boarding/validate";
    http.begin(url);
    http.addHeader("Content-Type", "application/json");
    http.setTimeout(5000);

    StaticJsonDocument<512> doc;
    doc["CredentialUid"] = token;
    doc["RawUid"]        = rawUid;
    doc["DeviceId"]      = deviceId;
    doc["DeviceType"]    = type;
    doc["TripId"]        = String(currentTrip.tripId);
    doc["RouteId"]       = String(currentTrip.routeId);
    doc["Timestamp"]     = getReadableTime();

    String body;
    serializeJson(doc, body);
    
    // Debug output
    if (DEBUG_API) {
      debugPrintApi(url, "POST", body, 0, "");
    }
    
    int code = http.POST(body);
    String resp = http.getString();
    
    // Debug response
    if (DEBUG_API) {
      debugPrintApi(url, "POST", body, code, resp);
    }

    if (code == 200) {
      StaticJsonDocument<512> r;
      if (deserializeJson(r, resp) == DeserializationError::Ok) {
        const char* result    = r["resultCode"]    | "UNKNOWN";
        const char* message   = r["message"]       | "";
        const char* studentId = r["studentId"]     | "";
        const char* resId     = r["residenceId"]   | "";
        const char* name      = r["studentName"]   | "";

        if (strcmp(result, "SUCCESS") == 0) {
          int seat = getNextSeatNumber();
          if (addBoardedStudent(String(studentId), token, String(resId))) {
            feedbackGranted(String(name), String(studentId), seat);
          } else if (isVehicleFull()) {
            feedbackOverload(String(name));
          }
        } else if (strcmp(result, "ROUTE_MISMATCH") == 0) {
          int seat = getNextSeatNumber();
          if (addBoardedStudent(String(studentId), token, String(resId))) {
            feedbackMismatch(String(resId), String(currentTrip.residenceId));
          } else if (isVehicleFull()) {
            feedbackOverload(String(name));
          }
        } else if (strcmp(result, "ALREADY_BOARDED") == 0) {
          feedbackDuplicate();
        } else {
          feedbackDenied(String(message));
        }
      } else {
        Serial.println("❌ Failed to parse JSON response");
      }
      http.end();
      return;
    }
    http.end();
  }

  // OFFLINE PATH
  if (manifestLoaded && manifestCount > 0) {
    String studentId, name, residenceId;
    String result = validateOffline(token, studentId, name, residenceId);

    if (result == "SUCCESS") {
      if (addBoardedStudent(studentId, token, residenceId)) {
        int seat = getNextSeatNumber() - 1;
        storeOfflineTap(token, rawUid, type, "SUCCESS", "Offline", true, false);
        feedbackGranted(name, studentId, seat);
      } else if (isVehicleFull()) {
        storeOfflineTap(token, rawUid, type, "OVERLOAD", "Full", false, false);
        feedbackOverload(name);
      }
    } else if (result == "ROUTE_MISMATCH") {
      if (addBoardedStudent(studentId, token, residenceId)) {
        storeOfflineTap(token, rawUid, type, "ROUTE_MISMATCH", "Mismatch", true, true);
        feedbackMismatch(residenceId, String(currentTrip.residenceId));
      } else if (isVehicleFull()) {
        storeOfflineTap(token, rawUid, type, "OVERLOAD", "Full", false, false);
        feedbackOverload(name);
      }
    } else if (result == "OVERLOAD") {
      storeOfflineTap(token, rawUid, type, "OVERLOAD", "Full", false, false);
      feedbackOverload("");
    } else if (result == "ALREADY_BOARDED") {
      storeOfflineTap(token, rawUid, type, "ALREADY_BOARDED", "Duplicate", false, false);
      feedbackDuplicate();
    } else {
      storeOfflineTap(token, rawUid, type, "UNKNOWN_CREDENTIAL", "Unknown", false, false);
      feedbackDenied("Not in manifest");
    }
  } else {
    storeOfflineTap(token, rawUid, type, "PENDING", "No manifest", false, false);
    feedbackOfflineStorage("PENDING");
  }
}

// ============================================================================
//  NFC HELPERS
// ============================================================================
bool selectAID() {
  uint8_t resp[64]; uint8_t len = sizeof(resp);
  if (!nfc.inDataExchange(SELECT_AID, SELECT_AID_LEN, resp, &len)) return false;
  if (len < 2) return false;
  return (resp[len-2] == 0x90 && resp[len-1] == 0x00);
}

bool extractToken(String &token) {
  if (!selectAID()) return false;
  uint8_t cmd[] = {0x00,0xCB,0x00,0x00,0x00};
  uint8_t resp[64]; uint8_t len = sizeof(resp);
  if (!nfc.inDataExchange(cmd, sizeof(cmd), resp, &len)) return false;
  if (len < 2) return false;
  if (resp[len-2] != 0x90 || resp[len-1] != 0x00) return false;
  int dataLen = len - 2;
  if (dataLen <= 0) return false;
  token = "";
  for (int i = 0; i < dataLen; i++) {
    if (resp[i] < 0x10) token += "0";
    token += String(resp[i], HEX);
  }
  token.toUpperCase();
  return token.length() > 0;
}

// ============================================================================
//  WIFI with DUAL NETWORK and STATIC IP SUPPORT
// ============================================================================
void fixMACAddress() {
  uint8_t mac[6]; uint64_t id = ESP.getEfuseMac();
  mac[0]=0xDE; mac[1]=0xAD; mac[2]=0xBE;
  mac[3]=(id>>0)&0xFF; mac[4]=(id>>8)&0xFF; mac[5]=(id>>16)&0xFF;
  WiFi.mode(WIFI_OFF); delay(100);
  esp_wifi_set_mac(WIFI_IF_STA, mac);
  WiFi.mode(WIFI_STA); delay(100);
}

bool connectToNetwork(const char* ssid, const char* password, bool useStaticIP = false) {
  Serial.printf("\n📡 Connecting to %s\n", ssid);
  
  if (useStaticIP && use_static_ip) {
    Serial.printf("   Using Static IP: %s\n", static_ip.toString().c_str());
    if (!WiFi.config(static_ip, gateway, subnet, dns)) {
      Serial.println("   Failed to configure Static IP, using DHCP");
    }
  }
  
  WiFi.begin(ssid, password);
  int attempts = 0;
  
  while (WiFi.status() != WL_CONNECTED && attempts < 40) {
    delay(500);
    Serial.print(".");
    attempts++;
  }
  
  Serial.println();
  
  if (WiFi.status() == WL_CONNECTED) {
    wifiConnected = true;
    Serial.printf("✅ WiFi OK — IP: %s\n", WiFi.localIP().toString().c_str());
    return true;
  }
  
  return false;
}

void connectToWiFi() {
  wifiConnected = false;
  
  // Try Primary Network (with Static IP if configured)
  if (connectToNetwork(primary_ssid, primary_password, true)) {
    Serial.println("✅ Connected to Primary Network (HGTS HQ)");
    return;
  }
  
  Serial.println("⚠️ Primary network failed, trying fallback...");
  
  // Try Fallback Network (Hotspot - always DHCP)
  if (connectToNetwork(fallback_ssid, fallback_password, false)) {
    Serial.println("✅ Connected to Fallback Network (Phone Hotspot)");
    return;
  }
  
  // Both networks failed
  wifiConnected = false;
  Serial.println("❌ All WiFi networks failed — OFFLINE mode");
}

void testAPIConnection() {
  if (!wifiConnected) { apiReachable = false; return; }
  String url = "http://" + String(serverHost) + ":" + String(serverPort) + "/api/health";
  http.begin(url); http.setTimeout(3000);
  int code = http.GET();
  apiReachable = (code == 200);
  http.end();
  Serial.printf("🌐 API: %s\n", apiReachable ? "✅ REACHABLE" : "❌ NOT REACHABLE");
}

// ============================================================================
//  SYSTEM STATUS
// ============================================================================
void printSystemStatus() {
  Serial.println("\n╔════════════════════════════════════════════╗");
  Serial.println("║           SYSTEM STATUS                   ║");
  Serial.println("╠════════════════════════════════════════════╣");
  Serial.printf("║ WiFi:      %s\n", wifiConnected ? "✅" : "❌");
  Serial.printf("║ API:       %s\n", apiReachable  ? "✅" : "❌");
  Serial.printf("║ PN532:     %s\n", nfcReady      ? "✅" : "❌");
  Serial.printf("║ Passengers: %d/%d\n", boardedCount, currentTrip.vehicleCapacity);
  Serial.printf("║ Next Seat:  %d\n", currentTrip.nextSeatNumber);
  Serial.printf("║ Offline Q: %d\n", offlineTapCount);
  if (hasActiveTrip && isTripActive()) {
    unsigned long rem = (currentTrip.endTime - millis()) / 60000;
    Serial.printf("║ Trip: %s (%lu min)\n", currentTrip.routeName, rem);
  } else {
    Serial.println("║ Trip: NONE");
  }
  Serial.printf("║ IP: %s\n", WiFi.localIP().toString().c_str());
  Serial.println("╚════════════════════════════════════════════╝");
}

// ============================================================================
//  SERIAL MENU
// ============================================================================
void showSerialMenu() {
  Serial.println("\n╔════════════════════════════════════════════╗");
  Serial.println("║  [1] Boarded   [2] Queue   [3] Clear Q    ║");
  Serial.println("║  [4] Clear All [5] Refresh [6] Status    ║");
  Serial.println("║  [7] Sync      [8] Debug ON/OFF [0] Exit  ║");
  Serial.println("╚════════════════════════════════════════════╝");
  Serial.print("👉 ");
}

void handleSerialMenu() {
  if (!Serial.available()) return;
  char c = Serial.read();
  while (Serial.available()) Serial.read();

  switch (c) {
    case '1':
      Serial.printf("👥 Boarded: %d/%d\n", boardedCount, currentTrip.vehicleCapacity);
      for (int i = 0; i < boardedCount; i++)
        Serial.printf("  %d. Seat %d - %s\n", i+1, boardedStudents[i].seatNumber, boardedStudents[i].studentId);
      break;
    case '2':
      Serial.printf("💾 Queue: %d\n", offlineTapCount);
      break;
    case '3':
      offlineTapCount = 0; saveOfflineTaps();
      Serial.println("✅ Queue cleared");
      break;
    case '4':
      clearBoardedStudents(); offlineTapCount = 0;
      saveOfflineTaps();
      manifestCount = 0; manifestLoaded = false;
      prefs.begin("manifest", false); prefs.clear(); prefs.end();
      Serial.println("✅ All cleared");
      break;
    case '5':
      if (wifiConnected && apiReachable) fetchActiveTripFromBackend();
      else Serial.println("⚠️ No connection");
      break;
    case '6': printSystemStatus(); break;
    case '7':
      if (wifiConnected && apiReachable) syncOfflineTaps();
      else Serial.println("⚠️ No connection");
      break;
    case '8':
      DEBUG_API = !DEBUG_API;
      Serial.printf("🔍 Debug mode: %s\n", DEBUG_API ? "ON" : "OFF");
      break;
    case '0': serialMenuActive = false; return;
    default: break;
  }
  if (c != '0') {
    delay(2000);
    showSerialMenu();
  }
}

// ============================================================================
//  SETUP
// ============================================================================
void setup() {
  Serial.begin(115200);
  delay(1000);
  bootTime = millis();

  Serial.println("\n╔════════════════════════════════════════════╗");
  Serial.println("║     HGTS NFC VALIDATOR v2.1 OPTIMIZED     ║");
  Serial.println("║        DUAL WIFI + STATIC IP READY        ║");
  Serial.println("║           DEBUG MODE AVAILABLE            ║");
  Serial.println("╚════════════════════════════════════════════╝");

  esp_err_t ret = nvs_flash_init();
  if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND) {
    nvs_flash_erase(); nvs_flash_init();
  }

  fixMACAddress();

  pinMode(LED_GREEN, OUTPUT); pinMode(LED_RED, OUTPUT);
  pinMode(LED_BLUE, OUTPUT);  pinMode(BUZZER_PIN, OUTPUT);
  allLedsOff(); digitalWrite(BUZZER_PIN, LOW);

  EEPROM.begin(EEPROM_SIZE);
  loadOfflineTaps();

  initDisplay();
  feedbackStartup();

  Serial.print("🔌 PN532... ");
  SPI.begin();
  nfc.begin();
  uint32_t ver = nfc.getFirmwareVersion();
  if (!ver) {
    Serial.println("❌ NOT FOUND");
    nfcReady = false;
  } else {
    Serial.printf("✅ v%d.%d\n", (ver>>16)&0xFF, (ver>>8)&0xFF);
    nfc.SAMConfig();
    nfcReady = true;
  }

  connectToWiFi();
  if (wifiConnected) {
    testAPIConnection();
    if (apiReachable) {
      fetchActiveTripFromBackend();
      fetchWeather();
    }
  }

  currentTrip.vehicleCapacity = 50;
  currentTrip.nextSeatNumber = 1;
  updateDisplay();
  printSystemStatus();
  Serial.println("\n✅ READY — tap a card or type 'menu'\n");
  Serial.println("💡 Tip: Type 'menu' and press 8 to toggle debug mode\n");
  lastActivityTime = millis();
}

// ============================================================================
//  LOOP
// ============================================================================
void loop() {
  unsigned long now = millis();

  syncTimeInBackground();

  // Sleep mode after inactivity
  if (!displaySleepMode && (now - lastActivityTime > INACTIVITY_TIMEOUT_MS) && !hasActiveTrip) {
    displaySleepMode = true;
    if (wifiConnected && apiReachable && (now - lastWeatherUpdate > WEATHER_REFRESH_INTERVAL_MS)) {
      fetchWeather();
    }
    updateDisplay();
  }
  
  if (hasActiveTrip && displaySleepMode) {
    displaySleepMode = false;
    updateDisplay();
  }

  // Periodic weather refresh in sleep mode
  if (displaySleepMode && wifiConnected && apiReachable && 
      (now - lastWeatherUpdate > WEATHER_REFRESH_INTERVAL_MS)) {
    fetchWeather();
    updateDisplay();
  }

  // Serial menu
  if (Serial.available()) {
    String in = Serial.readString(); in.trim(); in.toLowerCase();
    if (in == "menu") { serialMenuActive = true; showSerialMenu(); }
  }
  if (serialMenuActive) { handleSerialMenu(); return; }

  // WiFi watchdog with fallback network retry
  if (now - lastWifiCheck > 60000) {
    lastWifiCheck = now;
    if (WiFi.status() != WL_CONNECTED) {
      wifiConnected = false;
      Serial.println("⚠️ WiFi disconnected, reconnecting...");
      connectToWiFi();
      if (wifiConnected) { testAPIConnection(); }
    } else if (!apiReachable) {
      testAPIConnection();
    }
    if (wifiConnected && apiReachable) fetchActiveTripFromBackend();
    updateDisplay();
  }

  // Trip poll
  if (wifiConnected && apiReachable &&
      (now - lastConfigRefresh > CONFIG_REFRESH_INTERVAL_MS)) {
    lastConfigRefresh = now;
    fetchActiveTripFromBackend();
    updateDisplay();
  }

  // Heartbeat
  if (wifiConnected && apiReachable &&
      (now - lastHeartbeat > HEARTBEAT_INTERVAL_MS)) {
    lastHeartbeat = now;
    sendHeartbeat();
  }

  // Offline sync
  if (wifiConnected && apiReachable && offlineTapCount > 0 &&
      (now - lastSyncAttempt > SYNC_RETRY_INTERVAL_MS)) {
    lastSyncAttempt = now;
    syncOfflineTaps();
  }

  checkTripExpiry();

  // Tap debounce
  if (now - lastTapTime < POST_TAP_DELAY_MS) return;
  if (!nfcReady) { delay(100); return; }

  // Reset PN532 if stuck
  if (pn532FailCount > 10 && (now - lastPn532Reset > PN532_RESET_INTERVAL_MS)) {
    nfc.begin();
    nfc.SAMConfig();
    pn532FailCount = 0;
    lastPn532Reset = now;
  }

  uint8_t uid[7], uidLen;
  if (!nfc.readPassiveTargetID(PN532_MIFARE_ISO14443A, uid, &uidLen, 100)) {
    pn532FailCount++;
    delay(50);
    return;
  }
  
  pn532FailCount = 0;
  lastTapTime = millis();
  lastActivityTime = millis();
  
  if (displaySleepMode) {
    displaySleepMode = false;
    updateDisplay();
  }

  // Build raw UID string
  String rawUid = "";
  for (uint8_t i = 0; i < uidLen; i++) {
    if (uid[i] < 0x10) rawUid += "0";
    rawUid += String(uid[i], HEX);
  }
  rawUid.toUpperCase();

  // Try HCE (phone) first, fall back to physical card
  String token = "";
  bool isPhone = extractToken(token);
  if (isPhone) {
    sendToBackend(token, "PHONE", rawUid);
  } else {
    sendToBackend(rawUid, "CARD", rawUid);
  }
}