# The Memento Protocol (Giao Thức Dấu Ấn Memento)

**Phiên bản:** 1.0
**Áp dụng từ:** 2026-07-20
**Mục đích:** Chuyển giao được trạng thái có thể hành động của một dự án từ phiên chat AI cũ sang phiên chat AI mới mà không bắt chủ dự án phải kể lại toàn bộ lịch sử.

> Memento khong co nghia la copy nguyen chat cu. Memento la mot goi bang chung, quyet dinh, rang buoc va buoc tiep theo du de AI moi tai tao dung ngu canh, tu kiem chung do drift va tiep tuc an toan.

---

## 1. Ket qua bat buoc

Mot Memento Packet dat chuan khi AI moi co the tra loi dung, truoc khi sua bat ky file nao:

1. Du an dang o dau, tren branch va commit nao?
2. Production dang o trang thai nao, cai gi da deploy va cai gi chua?
3. Worktree ban do ai so huu, file nao tuyet doi khong duoc dong vao?
4. Quy dinh nghiep vu nao da duoc khach phe duyet, quy dinh nao con mo?
5. Viec dang do co nguyen nhan goc, bang chung va tieu chi nghiem thu gi?
6. Hanh dong ke tiep nho nhat, an toan nhat la gi?
7. Can doc file nao, chay lenh nao va khong duoc lam gi?

Neu mot cau khong co bang chung, packet phai gan nhan `UNVERIFIED` hoac `UNKNOWN`; khong duoc bien suy doan thanh su that.

---

## 2. Nguyen tac cot loi

### 2.1 Su that co thu bac

Khi hai nguon mau thuan, uu tien theo thu tu sau:

1. Bang chung production moi nhat co timestamp, request/transaction ID da che thong tin nhay cam va ket qua read-only.
2. Code dang o worktree + test dang chay + schema dang ap dung.
3. Quyết định mới nhất cua khach/BA duoc ghi ro ngay va pham vi.
4. `docs/CONTEXT_RECOVERY.md` va `task.md` moi nhat.
5. Changelog moi nhat.
6. Nhat ky lich su, chat cu va nho cua AI.

Khong nguon nao duoc phep vuot len bang chung moi hon chi vi no viet chi tiet hon.

### 2.2 Tach bon lop thong tin

Moi muc trong packet phai thuoc mot trong bon nhan:

| Nhan | Y nghia | Cach xu ly cua AI moi |
|---|---|---|
| `VERIFIED` | Da duoc kiem tra bang test, artifact, DB read-only hoac production evidence | Co the dua vao ke hoach, van can kiem drift neu deploystate co the thay doi |
| `DECIDED` | Da duoc chu du an, BA hoac khach chap thuan | Phai ton trong, khong tu doi nghiep vu |
| `IN_PROGRESS` | Co code/tai lieu dang do, chua dat gate | Doc code va tiep tuc tu gate dang do |
| `UNKNOWN/BLOCKED` | Chua co du lieu hoac can quyet dinh ben ngoai | Khong tu suy dien; neu can thi dat cau hoi ro rang |

### 2.3 Khong duoc dong nhat test voi nghiem thu

- Test local pass khong co nghia la artifact EXE/APK, VPS hay tien that da pass.
- Build pass khong co nghia la client dang chay dung commit.
- Endpoint `200` khong co nghia la dong tien, idempotency hay rollback dung.
- Chat cu khong thay the bang chung trong repo hay production.

### 2.4 Memento la ban ghi san sang hanh dong, khong phai ban ke chuyen

Khong ghi nhung chi tiet khong giup AI moi quyet dinh. Phai giu:

- cai da xac minh;
- cai chua xac minh;
- ly do cua quyet dinh;
- bang chung de kiem lai;
- buoc tiep theo va dieu kien dung.

---

## 3. Thanh phan cua Memento Packet

Moi packet phai co day du cac phan A den N. Khong duoc bo trong bang cach viet "xem chat cu".

### A. Identity va thoi diem

- Ten du an, workspace, nhanh Git, `HEAD`, upstream va ngay/gio tao packet.
- Ten tranche dang lam va muc dich kinh doanh/ky thuat.
- Nguoi quyet dinh nghiep vu, nguoi phe duyet deploy va nguoi build artifact neu khac nhau.

### B. Quy tac an toan khong duoc pha

- Cac file/module protected.
- Dirty worktree cua chu du an va pham vi AI duoc cham.
- Cam sua `.meta`, reset, clean, checkout hang loat, hardcode secret, deploy hay restart neu chua duyet.
- Loai du lieu cam ghi vao packet: password, token, HMAC secret, SSH key, raw customer PII, DB dump, URL co credential.

### C. Snapshot Git va ownership worktree

- Branch, HEAD, ahead/behind va commit dang phat trien.
- Danh sach file do AI tranche nay tao/sua.
- Danh sach file ban/asset/scene/tai lieu cua chu du an phai giu nguyen.
- Noi ro `UNTRACKED` nao thuoc tranche, `UNTRACKED` nao khong thuoc tranche.
- Khong rut gon thanh "worktree ban"; phai chi ra nhom file va rui ro.

### D. He thong va authority map

- Thanh phan nao la authority cua tung tai san/trang thai.
- Duong di cua request quan trong: client -> game backend -> web/ledger -> callback/realtime -> client.
- Moi truong dang active: local, test, staging, production; cai nao chi la candidate.
- Feature flag, public route, mode dormant/canary/open va nguoi/identity duoc phep dung.

### E. Production checkpoint

Chi ghi du lieu da duoc phep luu trong repo:

- release/commit/build label, migration da ap dung, route health va rollback artifact da che thong tin nhay cam;
- production da duoc dong vao hay chua;
- so du/transaction chi dung gia tri QA da duoc phep hoac gia tri da anonymize;
- hanh dong bi cam: vi du khong mo callback public, khong bat top-up, khong restart service.

Neu checkpoint co the cu, gan `VERIFY_BEFORE_ACTION` va ghi lenh/nguon can kiem lai.

### F. Quyet dinh nghiep vu va contract

Moi quyet dinh phai co:

- noi dung ro rang;
- nguon va ngay phe duyet;
- pham vi ap dung;
- he qua ky thuat;
- nhung cau hoi con mo.

Neu khach tra loi lai mot cau truoc do, phai ghi ro `SUPERSEDES <ngay/noi dung cu>` de AI moi khong chon nham ban cu.

### G. Cong viec da hoan thanh va bang chung

Voi moi tranche, ghi:

- file/module da doi;
- test/compile/build da chay;
- expected va actual;
- artifact/commit/checksum neu co;
- gia han cua bang chung: local, integration, artifact, production hoac customer acceptance.

### H. Van de dang mo va rui ro

Moi van de can co:

- muc do P0/P1/P2;
- symptom, pham vi, nguyen nhan da biet/chua biet;
- cach tai hien;
- dieu khong duoc lam de "chua chay";
- tieu chi dong van de.

### I. Buoc tiep theo co the thi hanh

Khong viet "tiep tuc lam wallet". Phai viet theo khuon:

`Muc tieu -> file/asset pham vi -> kiem tra truoc -> thay doi du kien -> test -> gate dat -> dieu kien dung/rollback.`

Chi co mot buoc `IN_PROGRESS` tai mot thoi diem.

### J. Lenh kiem tra va ket qua ky vong

Liet ke lenh co the chay an toan, working directory va ket qua mong doi. Tach ro:

- doc/diagnostic;
- test local;
- build artifact;
- production read-only;
- production change can phe duyet.

### K. Artifact va bang chung

- Link file trong repo, duong dan artifact, release label, report da che thong tin nhay cam.
- Transaction/request ID chi ghi dang rut gon/an danh neu can.
- Khong chen secret vao log, prompt hay changelog.

### L. Cau hoi can phan cong

Tach dung nguoi nhan:

- **Khach/BA:** quy tac tien, VIP, hoa hong, UX, fee/limit.
- **Web team:** API, referral tree, wallet/quy, payment state, provider settlement.
- **Game/backend:** atomicity, source attribution, idempotency, realtime, client artifact.
- **Chu du an:** phe duyet deploy, tien that, build, quyen truy cap.

### M. Prompt khoi dong cho AI moi

Packet phai ket thuc bang mot prompt copy/paste ngan, du ngu canh de AI moi bat dau dung thu tu.

### N. Chu ky va changelog

Ghi ro file nao da duoc cap nhat trong tranche: `task.md`, `CHANGELOG.md`, `docs/CHANGELOG.md`, `docs/CONTEXT_RECOVERY.md`, root `CONTEXT_RECOVERY.md` neu van con duoc dung.

---

## 4. Quy trinh dong phien cua AI cu

Truoc khi ket thuc mot tranche co thay doi, AI cu phai lam theo dung thu tu:

1. Dung cac thao tac dang chay; khong de process/deploy chua ro trang thai.
2. Kiem tra `git status --short`, branch, HEAD va diff cua pham vi vua sua.
3. Chay test/compile ty le voi rui ro; ghi ro test nao khong the chay va ly do.
4. Cap nhat cac tai lieu durable bang su that da xac minh, khong copy ket luan suy dien.
5. Gan nhan `VERIFIED`, `DECIDED`, `IN_PROGRESS` hoac `UNKNOWN/BLOCKED` cho moi noi dung quan trong.
6. Cap nhat Memento Packet theo template o phan 8.
7. Chay `git diff --check` cho cac file vua sua.
8. Kiem tra khong co secret/PII moi trong tai lieu, patch hay log.
9. Bao cho chu du an: da lam gi, chua lam gi, rui ro, gate tiep theo va co commit/push/deploy hay khong.

Neu conversation bi ngat dot ngot, AI sau phai uu tien doc durable artifacts truoc, khong dua vao doan chat bi thieu.

---

## 5. Quy trinh khoi dong cua AI moi

### 5.1 Khong sua file truoc khi dat baseline

AI moi phai:

1. Doc prompt Memento do chu du an dan vao.
2. Doc cac file bat buoc theo thu tu cua du an.
3. Chay `git status --short --branch` va `git log --oneline -n 8`.
4. Xac dinh file user-owned, file tranche-owned va file co nguy co conflict.
5. Doi chieu state tai lieu voi code/module dang active.
6. Bao cao ngan: `du an dang o dau`, `production`, `worktree`, `next gate`, `unknown`.
7. Chi sau do moi sua code khi co uy quyen phu hop.

### 5.2 Xu ly drift va mau thuan

Neu Memento noi A nhung code/production evidence noi B:

1. Khong rollback hoac "sua cho giong packet".
2. Danh dau packet cu la `STALE` o dung muc.
3. Thu thap bang chung read-only de xac dinh state moi.
4. Bao chu du an bang ngon ngu ro rang: cai gi da doi, tu khi nao neu biet, anh huong gi.
5. Cap nhat durable docs chi sau khi phan biet duoc fact voi suy doan.

### 5.3 Khong tu dong mo rong pham vi

Packet co the liet ke 10 backlog items, nhung AI moi chi lam muc dang `IN_PROGRESS` hoac muc chu du an vua yeu cau. Khong tu deploy, bat feature flag, chay tien that, dung credentials hay don worktree chi vi packet nhac den chung.

---

## 6. Quy tac bao mat cua Memento

Khong bao gio ghi vao Memento Packet, changelog, task hay prompt:

- password, token, API key, HMAC secret, SSH private key;
- raw database dump, raw production log co credential;
- email/username cua khach hang neu khong can thiet cho cong viec;
- full transaction body, payment address hoac personal data.

Thay vao do dung:

- ten bien moi truong, khong dung gia tri;
- duong dan backup root-only, khong dung noi dung backup;
- ID rut gon/an danh;
- mo ta quyen can co: "can SSH read-only da duyet";
- checksum, release label va ket qua test da che thong tin nhay cam.

Truoc khi ban giao, quet lai theo cac tu khoa: `password`, `secret`, `token`, `authorization`, `private key`, `DATABASE_URL`, `postgres://` va email khach hang.

---

## 7. Ap dung cho Y WONDER GREEN FARM

### 7.1 Nguon hien hanh

Trong repository nay, khong duoc tin mot file duy nhat. Thu tu bootstrap bat buoc la:

1. `RULES.md`
2. `docs/THE_MEMENTO_PROTOCOL.md`
3. `docs/MEMENTO_PACKET_CURRENT.md` neu co
4. `docs/CONTEXT_RECOVERY.md`
5. `task.md`
6. `CHANGELOG.md`
7. `docs/CHANGELOG.md`
8. `docs/API_CONTRACTS.md`
9. `docs/DB_SCHEMA.md`
10. `docs/WEB_GAME_BACKEND_JOURNEY.md`
11. `docs/SECURITY.md`

Sau do chay Git baseline. Khi task lien quan Unity, doc them `docs/DESIGN.md` truoc UI va module/scene lien quan. Khi task lien quan server/vi Point, doc module active, `server/package.json`, migration va test truoc khi de xuat sua.

### 7.2 Phan biet hai Context Recovery

- `docs/CONTEXT_RECOVERY.md` la **snapshot hien hanh**; cap nhat no o dau file khi co tranche quan trong.
- Root `CONTEXT_RECOVERY.md` la **nhat ky Memento lich su** va co the cham hon. Khong duoc dung no mot minh de quyet dinh production hay next task.
- Khi hai file lech nhau, doi chieu ngay, code, Git va bang chung; ghi ro file nao da bi supersede thay vi xoa lich su.

### 7.3 Gate bat buoc cua vi Point

Memento lien quan den tien phai tach ro bang chung o bon tang:

1. Test backend/store.
2. PostgreSQL/schema thuc.
3. Web outbox/HMAC/idempotency.
4. Client EXE/APK, relogin va cross-device.

Khong goi hoan thanh chi vi mot tang pass. Khong bat top-up, callback public, debit, migration balance hay tien that neu packet khong ghi ro phe duyet rieng va rollback.

---

## 8. Memento Packet Template

Copy block nay vao cuoi `docs/CONTEXT_RECOVERY.md` hoac vao prompt chuyen chat. Thay moi placeholder; xoa section khong ap dung chi khi ghi ro ly do.

```markdown
# MEMENTO PACKET

## 0. Identity
- Project/workspace: <path>
- Created at: <ISO-8601, timezone>
- Branch / HEAD / upstream state: <branch, commit, ahead-behind>
- Current tranche: <one sentence>
- Owner request: <latest user request>

## 1. Non-negotiable safety
- Must preserve: <user-owned paths / generated assets / protected modules>
- Never do without approval: <deploy, reset, clean, migration, real money, restart>
- Secrets/PII: never print or commit; use <approved secure channel if needed>.

## 2. Current truth
- VERIFIED: <fact + evidence path/command/date>
- DECIDED: <business decision + approver/date>
- IN_PROGRESS: <one active task + exact gate>
- UNKNOWN/BLOCKED: <fact missing + owner of next answer>

## 3. Git and ownership
- AI-owned changes this tranche: <files>
- User-owned dirty changes to leave untouched: <files/groups>
- Untracked files: <tranche vs unrelated>
- No commit/push/deploy status: <state>

## 4. Architecture and production
- Authority by asset/state: <component -> authority>
- Active release/build/migration: <sanitized labels>
- Feature modes and public exposure: <dormant/canary/open + routes>
- Rollback/checkpoint: <sanitized artifact/path or UNKNOWN>

## 5. Evidence
- Tests run: <command -> PASS/FAIL + scope>
- Builds/artifacts: <label -> result>
- Runtime/customer acceptance: <what was observed, where, when>
- Tests not run: <reason and risk>

## 6. Open risks and questions
- P0/P1/P2: <symptom, known cause, repro, completion criteria>
- BA/customer question: <exact question>
- Web/infra dependency: <exact contract or access needed>

## 7. Exact next action
- Goal: <one goal>
- Read first: <files>
- Expected edit scope: <files/modules>
- Verify with: <commands/test matrix>
- Stop/rollback condition: <condition>
- Requires owner approval?: <yes/no and why>

## 8. New-chat bootstrap instruction
Read the project bootstrap files in the mandated order, run Git baseline,
report project/production/worktree/next-gate status, and do not edit, deploy,
reset, clean, migrate or use money until the active task and ownership are clear.
```

---

## 9. Prompt chuyen sang phien chat moi

Chu du an chi can dan block sau, kem Memento Packet moi nhat neu packet khong nam trong repo:

```text
Tiep tuc <TEN DU AN> tai <WORKSPACE> theo The Memento Protocol.

Xung "be", goi toi "anh yeu".

Doc theo thu tu:
1. RULES.md
2. docs/THE_MEMENTO_PROTOCOL.md
3. docs/MEMENTO_PACKET_CURRENT.md (neu co)
4. docs/CONTEXT_RECOVERY.md
5. task.md
6. CHANGELOG.md
7. docs/CHANGELOG.md
8. docs/API_CONTRACTS.md
9. docs/DB_SCHEMA.md
10. docs/WEB_GAME_BACKEND_JOURNEY.md
11. docs/SECURITY.md

Sau do chay git status --short --branch va git log --oneline -n 8.
Chua sua file, chua deploy va chua dong vao production truoc khi bao cao:
- du an dang o dau;
- production dang o dau;
- dirty worktree nao cua toi phai giu;
- mot next gate cu the;
- unknown/blocker can xac minh.

MEMENTO PACKET:
<DAN PACKET MOI NHAT VAO DAY NEU CAN>
```

---

## 10. Definition of Done

Mot tranche chi duoc coi la ban giao tot khi:

- AI moi co the bat dau tu repo va packet ma khong can hoi lai lich su co ban.
- Quy dinh nghiep vu, production state va dirty ownership khong bi lap lom hay mau thuan im lang.
- Bang chung du de phan biet da test, da build, da deploy va da nghiem thu.
- Secrets va du lieu ca nhan khong nam trong tai lieu.
- Next action la mot gate cu the, co tieu chi pass/fail va dung dung pham vi.
- Tai lieu khong bat AI moi phai tin mo quang; no chi cho AI moi cach tu kiem chung.

Memento thanh cong khi phien chat moi khong "nho" may moc, ma co the **tai tao dung su that va tiep tuc dung viec**.
