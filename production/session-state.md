# Trạng thái — 14/11/2026

Project Unity 6000.0.62f1 / HDRP 17.0.4. Đã thay game web cũ bằng nông trại Unity.

- 31 file C#; core tests đạt 66/66 assertion, native menu/interaction harness đạt.
- Windows Player build thành công: FARM_BUILD_SUCCEEDED trong Logs/build.log.
- Native Player chạy với GPU và chụp PNG thực tế; gameplay-latest.png và shop.png đã được mở kiểm tra.
- Native interaction harness đạt: nút thao tác, catalog ngô, tưới, thưởng nhiệm vụ một lần và lưu thưởng. Marker FARM_PRESENTATION_INTERACTION_PASSED trong Logs/presentation-verified.log. Harness gọi callback UI; không thay thế human playtest bàn phím/chuột.
- HUD mới: cửa hàng, kho, vật nuôi, tài nguyên, nhiệm vụ, minimap; cảnh procedural có ngô, cỏ, nhân vật, máy cày, chuồng, ao, nhà và núi.
- Launcher demo tạo save riêng và rebase thời gian ở lần mở đầu.
- Repo bàn giao theo yêu cầu: https://github.com/hoangquann147/nextgen-farm . Bản Windows được đóng gói riêng cho GitHub Releases.

Giới hạn: hình ảnh prototype stylized, chưa đạt độ chi tiết game tham chiếu; chưa co-op, Town Hub đầy đủ, mua mở rộng đất hoặc nước vật lý. Chưa đo FPS/GPU Frame Debugger. Không khẳng định đã xem toàn bộ video; đã đối chiếu ảnh người dùng và các storyboard công khai.

## Cập nhật flow/menu

Đã thêm FarmGameFlow, FarmSettings, FarmAudio và FarmFlowChecks (31 file C# tổng cộng). Unity compile/build và native harness cuối đều đạt. Có sảnh nền farm quay chậm, free-look không cần giữ chuột, Alt/Tab đổi con trỏ, Esc đóng catalog/menu, cài đặt âm thanh/quality/FPS lưu riêng. Đã xem ảnh main-menu/settings trong production/qa/evidence/menu-flow. Chưa xác nhận photorealistic hoặc human playtest camera/audio.

Video local mới đã được xem bằng contact sheet các mốc trải suốt clip; ghi chú tại production/art-reference.vi.md, file tham chiếu nằm trong .tools (không ship).
