# Memento Packet Hien Hanh - Y WONDER GREEN FARM

**Packet ID:** `ywgf-2026-07-20-memento-v1`
**Tao luc:** 2026-07-20 (Asia/Ho_Chi_Minh)
**Loai:** Snapshot ban giao de bat dau phien AI moi; khong phai bang chung production thay the kiem tra live.
**Giao thuc:** doc `docs/THE_MEMENTO_PROTOCOL.md` truoc khi dua packet nay vao hanh dong.

---

## 1. Identity va muc tieu dang lam

- **Project/workspace:** `D:\LamGameUnity\BaChuKhuRung3D` - Y WONDER GREEN FARM.
- **Git baseline da kiem tra:** branch `codex/backend-mvp`, `HEAD e9282903` (`feat: complete dedicated QA Point canary`). Upstream can drift; phien moi phai chay `git status --short`, `git log -3 --oneline` truoc khi sua.
- **Muc tieu kinh doanh lon:** Point tren web va Point trong game la mot so du chung; PostgreSQL game la ledger Point authoritative. Khong mo tien that, debit, link/migrate legacy hay deploy chi dua vao packet nay.
- **Muc tieu tranche hien tai:** hoan thien contract source-lot, hoa hong va VIP tren local/isolated PostgreSQL truoc; giu wallet production dormant va co gate ro rang.
- **Muc tieu cua packet nay:** de phien AI moi tai tao dung trang thai, ownership, rui ro va next gate ma khong can nguoi dung ke lai chat cu.

## 2. Bat buoc doc va cach kiem chung

Doc theo thu tu nay truoc khi sua file hoac ket luan production:

1. `RULES.md`
2. `docs/THE_MEMENTO_PROTOCOL.md`
3. `docs/MEMENTO_PACKET_CURRENT.md`
4. `docs/CONTEXT_RECOVERY.md` (snapshot hien hanh)
5. `task.md` (uu tien va backlog hien hanh)
6. `CHANGELOG.md` va `docs/CHANGELOG.md`
7. `docs/API_CONTRACTS.md`, `docs/DB_SCHEMA.md`, `docs/WEB_GAME_BACKEND_JOURNEY.md`, `docs/SECURITY.md`
8. Phan code/subsystem lien quan, sau do moi lap ke hoach hay sua.

**Nguon su that:** bang chung production moi nhat > code/schema/test dang chay > quyet dinh BA/khach moi nhat da ghi tai lieu > recovery/task/changelog > chat cu. Moi diem chua du bang chung phai gan `UNKNOWN`, khong duoc suy doan.

## 3. Rang buoc an toan va ownership worktree

### Tuyet doi khong lam

- Khong `git reset --hard`, `git checkout --`, `git clean`, revert hang loat hay xoa asset hang loat.
- Khong sua/xoa `.meta` cua Unity, ke ca khi thay meta bi modified/deleted trong `git status`.
- Khong dua password, SSH key, token, cookie, HMAC, email ca nhan hay ID nguoi choi vao repo, log, packet, commit hay chat handoff.
- Khong restart/deploy/thay env VPS, bat top-up/debit, link/migrate balance, hoac dung tien that neu khong co duyet rieng cho dung operation.

### Worktree dang ban - phan lon la cua chu du an

- **Unity/City user-owned:** `Assets/_Project/_Scenes/CityScene.unity`, `Assets/_Project/_Scenes/FarmScene.unity`; `Assets/Building/NEWMap/Map2.1/**` (materials, model va meta); `Assets/HDRI/**`; `Assets/Test/Male/SwimIdle.fbx`, `swim.fbx` va meta lien quan. Day la city/Map2.1/material/animation dang duoc anh yeu cap nhat; chi dong vao khi anh yeu giao dung task ro rang.
- **Backend/source-lot tranche khac dang do:** `server/store.js`, `server/postgresStore.js`, `server/schema.sql`, `server/package.json`, `server/migrations/007_point_source_ledger.sql`, `server/pointSourceLedger.js`, `server/pointSourceLedgerIntegrationTest.js` va cac tai lieu Point. Khong gom vao commit Memento, khong sua/revert neu chua audit pham vi.
- **Tai lieu/asset khac khong ro owner:** vi du `docs/WEB_ITEM_NOTIFICATION_API_HANDOFF.md`. Xem la user/team-owned cho den khi da doi chieu.

Neu co file thay doi giua luc lam, dung lai, doc diff va lam viec voi thay doi do; khong coi no la rac can xoa.

## 4. Production va wallet - trang thai da co bang chung lich su

### VERIFIED (snapshot tai lieu, can live-verify neu sap thao tac remote)

- Authority wallet v3 da duoc deploy o che do dormant; public callback van phai la `404`, debit wallet tat va khong mo cho user dai tra.
- Canary ky thuat khong tien tren identity QA rieng da pass: web va client cung thay doi `5000 -> 5053` Point sau mot swap synthetic; replay khong cong doi; EXE/APK logout/login va thay phien deu giu cung so du. Day **khong** phai canary tien that.
- Sau remediation lich su, bang chung client ghi nhan `4367 Point` va EXE/APK thay phien dung. Con so nay la snapshot cua mot account/cot moc cu, khong duoc coi la so du live hien tai cua bat ky ai.
- Source-lot Point local: adapter/ledger/migration candidate `007` da co va test JSON pass; `server/postgresSmokeTest.js` da them migration `007` va coverage race idempotency, conflict, FIFO, `UNATTRIBUTED`, pool restart va balance-invariant. `node --check` pass, nhung PostgreSQL integration chua chay do workstation khong co Docker, PostgreSQL client/server hay WSL distro. Migration chua apply, runtime callback/shop/reservation/transfer chua mutate lots. Khong deploy hoac restart tu tranche nay.

### DECIDED (BA/khach da chot, implementation con dang do)

- Web Point va game Point la mot vi chung; game PostgreSQL `player_economy.pos` la balance authoritative.
- Ty gia current: `1 USDT = 26.5 Point`, `1 YWH = 1.59 Point`; moi lot can giu rate version/rate goc de doi soat sau nay.
- Point nguon USDT tieu trong game tra hoa hong USDT; Point gameplay tai tieu dung tra hoa hong Point. FIFO theo source-lot, transfer giu lineage/source/rate, cho phep so le.
- Hoa hong 6 cap: truc tiep `8%`, nam cap sau moi cap `1%`, tong toi da `13%`.
- VIP: cong don nguon USDT toi `2,650 Point`. Ban tra loi moi nhat: Point nguon USDT duoc chuyen toi van tinh VIP khi nguoi nhan tieu; refund phai tru tien do va thu hoi VIP neu roi duoi nguong.
- Hoa hong phat sinh vao be khoa neu A hoac B chua VIP; chi mo su dung/rut khi ca A va B VIP. Khi du dieu kien, mo cac khoan lich su tuong ung. Payout chi mo sau thoi gian cho toi thieu khoang 10 phut va giao dich da final thanh cong.

### IN_PROGRESS / UNKNOWN

- Chua co commission outbox, VIP pool, reversal policy hoan chinh va fractional spending production. `pos` production van la so nguyen.
- Gate PostgreSQL source-lot dang blocked boi moi truong local: phai cap PostgreSQL disposable va DSN rieng, sau do chay `npm.cmd run test:postgres --prefix server`; test tu tao/drop schema `ywtest_*`, khong duoc dung DSN production.
- Chua co bang chung live moi hon cho trang thai VPS sau khi packet duoc tao. Truoc remote action, phai chay read-only preflight va doi chieu release/env/health/ledger theo `docs/SECURITY.md`.
- Cac quyet dinh con phai chot truoc release commission: precision UI/ledger, xu ly hoa hong da mo da bi tieu khi refund dac biet sau final, nguon quy USDT/Point, quy tac phi-han muc rut va UI hien thi VIP/be khoa.

## 5. Gameplay va Unity - cong viec dang do

### P0 placement thu

- Loi runtime tung thay: tui co 7 thu, tha 1 con vao chuong ma tui van 7. Backend atomic test pass khong du de nghiem thu artifact that.
- Phan loai: chuong hang rao moi di qua `HandleEnclosureAnimalSelectedAsync` va `/player/farm/animals/place`; `AnimalPenSpawner` cu di qua `HandlePenAnimalSelected` va luong local cu.
- Gate nghiem thu build moi: `N -> N-1` ngay khi server chap nhan; chi spawn mot con; retry/close app khong tru doi/khong spawn doi; fail khong mat con; relogin va EXE -> APK -> EXE van dung. Tester QARich/Alina01 tung dung **build cu**, do do phai xac minh artifact co client atomic moi truoc khi ket luan da sua.

### Cac loi/gameplay da ghi nhan, chua tu y sua trong tranche wallet

- Cum chanh day: tile slave co the con `Plowed` du `masterTile != null`; UI co the bat animation truoc khi `InteractPlant` tra false; can toast that bai va hoan hat ro rang; `FindNearbyPlowedTiles` can rang buoc khoang cach/lien ke.
- Chat cay: tai nguyen da bien mat nhung o gan van hien UI chat, bam lai co the cong them go. Day la ung vien loi local interaction/state/despawn va anti-duplicate server; chua co bang chung du de quy ket, can trace request va local collider/state.
- Shop popup city: trigger nho nhung popup co the bat khi nguoi choi chua cham vao toa nha; can inspect collider hierarchy, player collider/radius va code `OnTriggerEnter/Stay`, khong doan la backend.
- SwimIdle import: clip `Layer0` zero frame va copied avatar hierarchy mismatch. Day la asset/rig import, khong phai loi code game cho den khi co artifact FBX dung.

## 6. Ket qua da dat va bang chung can biet

- Kiem tra Memento tranche nay: `git diff --check` cho cac tai lieu Memento phai pass; scan tai lieu moi khong chua gia tri secret/credential.
- Ket qua test wallet, canary, remediation va source-lot nam trong `docs/CONTEXT_RECOVERY.md`, `task.md` va cac test/bao cao da duoc ghi o do. Khong tuyen bo da rerun chung trong tranche viet Memento.
- Moi test/deploy sau nay phai luu: commit/build/checksum, testcase expected/actual, request/transaction ID da redact, balance truoc/sau o web-PostgreSQL-client, log loi/root cause/regression test va ket qua rollback.

## 7. Buoc tiep theo dung thu tu

1. **Khong deploy:** provision PostgreSQL disposable/isolated (duoc phep ro rang) va chay migration `007` + integration suite source-lot; neu local chua co PostgreSQL/Docker/WSL thi bao block va xin duyet cach tao moi truong, khong ep cai dat len may/VPS.
2. Hoan thien contract du lieu hoa hong/VIP thanh ADR/API/schema co version, bao gom precision, source lots, reservation, delayed-finalization va reversal truoc khi mutation production.
3. Build EXE/APK moi tu commit co client atomic va nghiem thu P0 placement theo ma tran o muc 5. Khong cho tester dung artifact cu.
4. Chi sau cac gate tren va duyet rieng: canary tien that cuc nho voi mot identity QA, full transaction trace, retry/idempotency va rollback. Khong dung tai khoan/vi khach hang.

## 8. Prompt khoi dong cho phien AI moi

```text
Tiep tuc Y WONDER GREEN FARM tai D:\LamGameUnity\BaChuKhuRung3D.
Xung "be", goi toi la "anh yeu".

Truoc khi lam bat ky thay doi nao:
1. Doc RULES.md, docs/THE_MEMENTO_PROTOCOL.md, docs/MEMENTO_PACKET_CURRENT.md,
   docs/CONTEXT_RECOVERY.md, task.md, CHANGELOG.md, docs/CHANGELOG.md,
   docs/API_CONTRACTS.md, docs/DB_SCHEMA.md, docs/WEB_GAME_BACKEND_JOURNEY.md,
   docs/SECURITY.md theo thu tu.
2. Chay git status --short va git log -3 --oneline; bao cao branch, HEAD, worktree
   va ownership truoc khi sua.
3. Khong reset/clean/checkout/revert hang loat. Khong sua/xoa .meta. Khong dua secret
   vao repo. Khong dong vao Map2.1/scene/HDRI/animation cua anh neu khong duoc giao ro rang.
4. Phan biet VERIFIED, DECIDED, IN_PROGRESS va UNKNOWN. Production phai live-verify
   truoc remote action; khong tu bat wallet/debit/top-up/deploy/tien that.

Muc tieu truoc mat: [dien task duoc anh giao trong phien moi].
Hay bao cao plan ngan gon, sau do thuc hien den khi co bang chung nghiem thu.
```

## 9. Dieu kien lam moi packet

Cap nhat packet nay ngay sau bat ky su kien nao sau day: commit/merge/deploy/rollback, build EXE/APK duoc nghiem thu, migration/schema thay doi, quyet dinh BA/khach moi, loi P0 moi, thay doi ownership worktree, canary/transaction, hoac truoc khi ket thuc mot phien AI dai.

Khi cap nhat, ghi ngay/gio, evidence path hoac lenh read-only co the lap lai; de `UNKNOWN` neu chua xac minh. Khong copy secret, token, password, email ca nhan hay raw log nhay cam vao packet.
