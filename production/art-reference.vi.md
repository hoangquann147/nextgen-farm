# Đối chiếu video gameplay người dùng cung cấp

Đã trích và mở xem contact sheet gồm các mốc khoảng 50, 150, 250, 350, 450 và 550 giây từ video local dài 9:53.96. Đây là xem khung hình trải suốt clip, không phải xác nhận đã nghe/xem liên tục toàn bộ video. Video/khung hình tham chiếu được giữ trong .tools và không đưa vào source/release.

Nhận xét quan sát:
- Phong cách stylized, màu vàng/xanh ấm, không phải photorealistic.
- Camera theo sát nhân vật; có thể nhìn rõ cây trồng, thao tác và ô mục tiêu.
- Cỏ phủ dày, lá cây mềm, cây trồng có silhouette lớn và dễ đọc.
- Đất có đá nhỏ/rãnh cày, bờ đất và đường lát tách vùng rõ.
- Sảnh chính đặt menu dọc bên trái; nút màu kem nằm trong khung gỗ, có màu xanh cho lựa chọn.
- Gameplay dùng biểu tượng tưới/thu hoạch nổi trên cây và bảng hướng dẫn ở mép phải.

Đối chiếu bản Unity:
- Có góc nhìn thứ ba, cây trồng theo ô, chỉ báo tưới, HUD, minimap và catalog.
- Đã bổ sung sảnh, cài đặt và free look/cursor state theo yêu cầu bằng component runtime.
- Model, texture địa hình, cỏ, animation và mức độ hoàn thiện còn cách xa tham chiếu. Camera/menu code không tự tạo ra asset photorealistic.
- FarmVisualLibrary có farmerPrefab, tractorPrefab, cowPrefab, mesh/material các giai đoạn cây và vật liệu đất khô/ướt để thay bộ procedural bằng asset có license phù hợp. Chưa nhập bộ asset bên ngoài hoặc xác nhận đạt photorealistic.
