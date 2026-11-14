# Báo cáo kiểm chứng — 14/11/2026

Unity 6000.0.62f1 / HDRP 17.0.4, Windows x64.

| Kiểm tra | Kết quả |
| --- | --- |
| Core tests chạy lại trước đóng gói | 66/66 assertion đạt |
| Lõi được kiểm tra | Trồng/tưới/thu hoạch, offline, save/backup, chuồng, đường đi, địa hình, thưởng một lần, auto navigation |
| Quét 3×3 trên bản đồ chữ nhật | 289 kích thước, không bỏ sót ô trong vùng không có vật cản |
| Biên dịch Unity và Windows build hiện tại | 31 file C#, FARM_BUILD_SUCCEEDED |
| Native Player / ảnh chụp | FARM_NATIVE_CAPTURE_PASSED; đã mở gameplay-latest.png và shop.png kiểm tra hình ảnh |
| Native harness tương tác | FARM_PRESENTATION_INTERACTION_PASSED: context button, seed catalog, watering, one-time reward, save persistence |
| Play Mode smoke trước đợt nâng cấp hình ảnh | Đạt bootstrap, thao tác 3×3, chuồng/save, auto tractor thay đổi Transform; không coi đó là test lại toàn bộ bản hình ảnh mới |
| Human playtest / âm thanh / benchmark FPS | Chưa xác nhận |
| Co-op | Chưa triển khai |

Core dùng Roslyn và .NET Framework. Native harness gọi callback UI trong Player với save riêng, không ghi lên nông trại người dùng. Logs nằm trong Logs/ (gitignored). Bằng chứng hình ảnh nằm tại production/qa/evidence/presentation-v2/.

Lệnh: Verification/run.ps1; Tools/StartFarm.ps1 -Mode Build. Native Player dùng -farmCapture, -farmCaptureAndQuit và -farmVerifyPresentation, với NEXTGEN_FARM_SAVE_DIR trỏ tới thư mục kiểm thử riêng.

## Cập nhật camera/menu — 14/11/2026

- 31 file C# đã biên dịch trong Unity và build Windows thành công.
- Native Player chạy lại trên mã cuối với -farmVerifyFlow và -farmVerifyPresentation: exit 0, hai marker PASS.
- Flow harness đã gọi callback Play/Settings/Return, kéo giá trị slider bằng API UI, kiểm tra chuyển Pointer/catalog/pause, bốn preset HDRP, các FPS cap, dữ liệu không hợp lệ và JSON settings.
- Kiểm tra AudioSource.volume xác nhận Master nhân Music/SFX độc lập; chưa nghe và đánh giá âm thanh bằng human playtest.
- Ảnh sảnh và cài đặt: production/qa/evidence/menu-flow/main-menu.png, settings.png. Đã mở ảnh để kiểm tra bố cục.
- Các test cũ về thao tác/cửa hàng/tưới/thưởng/save vẫn đạt trong cùng lượt native Player.
- Các kiểm tra này không xác nhận cảm giác xoay chuột vật lý, độ ổn định FPS, chất lượng photorealistic hoặc co-op.
- Video local người dùng gửi đã được xem qua các khung hình trải suốt khoảng 50–550 giây; không công bố video/khung hình tham chiếu trong repo.

Đóng gói cuối: đã giải nén ZIP Windows, chạy Tools/StartDemo.ps1 bằng Windows PowerShell 5 với save demo mới, vào sảnh và tạo ảnh native thành công (Logs/menu-packaged.log). File assembly trong ZIP khớp SHA-256 bản build. Chưa xác nhận tương tác chuột/bàn phím vật lý bằng human playtest.
