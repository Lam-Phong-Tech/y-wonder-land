# VPS Game Read-only Audit - 10/07/2026

## Pham vi

- VPS: `42.96.18.14`.
- Dang nhap mot lan bang `root` de chay lenh chi doc.
- Khong cai dat goi, khong sua firewall, khong them SSH key, khong thay doi service.
- Mat khau va private key khong duoc ghi vao repo.

## Ket qua he thong

| Hang muc | Ket qua |
|---|---|
| He dieu hanh | Ubuntu 22.04.5 LTS (Jammy), khong phai 24.04 |
| Kernel | Linux 5.15.0-177-generic, x86-64 |
| Ao hoa | KVM/QEMU |
| CPU | 2 vCPU |
| RAM | 3.8 GiB, khoang 3.4 GiB available luc audit |
| Swap | 3.8 GiB, chua su dung |
| Disk | 50 GB, phan vung root con khoang 37 GB |
| Timezone | Asia/Ho_Chi_Minh |
| Dong bo gio | NTP active, system clock synchronized |
| Uptime luc audit | Khoang 9 gio 26 phut |

## Network va bao mat hien tai

- Public IP gan truc tiep tren `ens18`: `42.96.18.14/26`.
- Chi co SSH `22/tcp` dang listen tren IPv4/IPv6.
- UFW dang active, policy mac dinh `deny incoming`, `allow outgoing`.
- Rule inbound duy nhat la `22/tcp` tu moi dia chi.
- Khong co failed systemd unit tai thoi diem audit.
- SSH server: `OpenSSH_8.9p1 Ubuntu-3ubuntu0.15`.
- Host fingerprint ED25519 da kiem tra: `SHA256:dpAuiUAA7K0h3iDDGxys6XOpHv1uVRGUSW4y23qYgZk`.

## Phan mem hien co

- Co Git `2.34.1`.
- Chua co Node.js/npm.
- Chua co PostgreSQL client/server.
- Chua co Caddy hoac Nginx.
- Chua co Docker.
- Khong thay dich vu ung dung cu can bao ton.

## Danh gia

VPS **du cho demo khoang 20 nguoi dong thoi** voi kien truc Node game-server + PostgreSQL + Caddy, neu chi chay cac dich vu cua du an va co gioi han log/backup hop ly. Cau hinh nay chua phai production lon, nhung phu hop staging/demo hien tai.

Ubuntu 22.04.5 LTS van duoc ho tro va phu hop voi Node/PostgreSQL/Caddy. Khuyen nghi **giu 22.04 de khong cham deadline**. Chi rebuild len 24.04 neu cong ty bat buoc chuan OS; neu rebuild thi phai lam truoc khi tao database va du lieu that.

## Rui ro can xu ly truoc khi deploy

1. `root` + password dang mo qua Internet. Can tao user deploy rieng, cai SSH key, test dang nhap thanh cong roi moi han che root/password.
2. Chua co Node, PostgreSQL, reverse proxy, service auto-start, backup hay monitoring.
3. Chua mo `80/443`; chi mo sau khi reverse proxy da san sang. Khong public `3000` va `5432`.
4. `api.ywonder.net` van chua tro ve VPS nay.

## Cong viec ke tiep de xuat

1. Xin chap thuan tao `deploy` user + SSH key va hardening SSH co rollback.
2. Update goi he thong; cai Node.js LTS, PostgreSQL va Caddy.
3. Tao database/user rieng, secrets trong file env tren VPS, khong commit.
4. Hoan thien va test `postgresStore` tren source truoc khi chuyen `STORE_MODE=postgres`.
5. Deploy Node bind `127.0.0.1:3000`, service auto-start; Caddy public `80/443` va proxy WebSocket.
6. Test bang URL staging/IP truoc; chi doi DNS/Unity sau khi REST + WSS + restart + backup deu pass.
