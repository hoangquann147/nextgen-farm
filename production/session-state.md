# Trạng thái — 14/11/2026

Project Unity 6000.0.62f1 / HDRP 17.0.4. Đã thay game web cũ bằng nông trại Unity.

- 27 file C#; core tests chạy lại đạt 66/66 assertion.
- Windows Player build thành công: FARM_BUILD_SUCCEEDED trong Logs/build.log.
- Native Player chạy với GPU và chụp PNG thực tế; gameplay-latest.png và shop.png đã được mở kiểm tra.
- Native interaction harness đạt: nút thao tác, catalog ngô, tưới, thưởng nhiệm vụ một lần và lưu thưởng. Marker FARM_PRESENTATION_INTERACTION_PASSED trong Logs/presentation-verified.log. Harness gọi callback UI; không thay thế human playtest bàn phím/chuột.
- HUD mới: cửa hàng, kho, vật nuôi, tài nguyên, nhiệm vụ, minimap; cảnh procedural có ngô, cỏ, nhân vật, máy cày, chuồng, ao, nhà và núi.
- Launcher demo tạo save riêng và rebase thời gian ở lần mở đầu.
- Repo bàn giao theo yêu cầu: https://github.com/hoangquann147/nextgen-farm . Bản Windows được đóng gói riêng cho GitHub Releases.

Giới hạn: hình ảnh prototype stylized, chưa đạt độ chi tiết game tham chiếu; chưa co-op, Town Hub đầy đủ, mua mở rộng đất hoặc nước vật lý. Chưa đo FPS/GPU Frame Debugger. Không khẳng định đã xem toàn bộ video; đã đối chiếu ảnh người dùng và các storyboard công khai.
