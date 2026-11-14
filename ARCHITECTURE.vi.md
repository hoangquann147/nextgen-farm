# Kiến trúc NextGen Farm

## Các lớp đang được triển khai

```text
Keyboard/Mouse hoặc HUD
        |
TractorController / FarmBootstrap
        |
FarmGridManager / AnimalPenSystem ---- SeasonTimeManager
        |                                  |
FarmWorld + AnimalWorld + Wallet <----- Now (UTC seconds)
        |                  |
        |                  +---- FarmSnapshot ---- FarmSaveStore
        |                                        JsonUtility + file/backup
        +---- Changed(index)
                 |
        FarmInstanceRenderer (chunk cache)
                 |
        RenderMeshInstanced + HDRP materials
```

`FarmBootstrap` là composition root: đọc và xác thực save vào model tạm; chọn farm mới nếu chưa có save; khởi tạo đồng hồ; inject model vào các adapter; dựng view; bật autosave. Model không tham chiếu UnityEngine và không đọc input, filesystem, clock toàn cục hoặc shader. Hành động nhận `now` tường minh giúp kiểm thử các mốc thời gian và cho phép host authoritative sau này.

Mỗi ô hiện chỉ có trạng thái: `tilled`, `blocked`, `height`, ID cây, mốc thời gian cây, mốc đất khô và cờ tưới của vụ. Màu đất được suy ra từ thời gian, tránh phải serialize “đất ướt” đã lỗi thời sau khi offline. Cây chín vẫn giữ nguyên cho tới harvest. Định nghĩa content không lưu cùng world: ID phải ổn định xuyên phiên bản.

Wallet và crop/animal mutation chạy tuần tự trên main thread. Harvest đưa sản phẩm vào kho trước khi xóa cây/reset chuồng. Gieo/cho ăn kiểm tra điều kiện rồi mới trừ tiền. Vùng 3×3 là chuỗi giao dịch ô độc lập có thứ tự cố định, không giả định toàn vùng thành công. Các thao tác thất bại không phát `Changed`.

Renderer nghe sự kiện thay đổi ô để đánh dấu chunk bẩn. Mỗi chunk tính mốc thời gian trình bày tiếp theo trong lúc dựng ma trận: đất khô, giai đoạn cây, cây chín. Không quét mô phỏng mỗi frame cho hàng nghìn cây. Vẫn gửi các batch render mỗi frame; bộ đệm ma trận tồn tại lâu dài. Không có per-tile `Update`, coroutine hoặc collider. Nhân vật demo đọc chiều cao lưới để di chuyển; chưa phải nhân vật vật lý dùng CharacterController.

## Đồng hồ và offline

```text
readyUtc = plantedUtc + growSeconds - waterBonus
ready = now >= readyUtc
wet = now < wetUntilUtc
season = floor((now - epochUtc) / 1020) mod 4
```

Không cộng lại “thời gian offline” vào deadline: làm vậy sẽ tính hai lần khi deadline vốn đã là UTC. Không chạy vòng lặp bù các tick đã bỏ lỡ. Chuồng một feed/production/harvest cycle, nên kết quả của 1 giờ offline và 1 năm offline đều là một lần sản phẩm đã sẵn sàng, nếu chưa được thu hoạch.

Trong phiên dùng `Time.realtimeSinceStartupAsDouble` để tránh sửa đồng hồ OS làm cây quay ngược. Khởi động/resume đọc UTC mới. Không có chống gian lận thời gian local; khi có multiplayer, `Now` phải đến từ host/server chứ không nhận từ client.

## Thiết kế mở rộng chưa được triển khai

### Co-op

Hiện **chưa có code mạng**. Khi triển khai, giữ một host duy nhất sở hữu world, đồng hồ và save. Dùng Netcode for GameObjects hoặc transport đã xác minh với phiên bản project; không dùng `FarmGridManager.Act` để client tự ghi state.

Client gửi command gồm `requestId`, tool, crop ID và target cell; host lấy player ID từ connection, kiểm tra khoảng cách, trạng thái đi bộ/máy cày, tốc độ thao tác, quyền sửa farm và tiền. Host tự tính radius và `now`; không tin client truyền yield, price, timestamp hay vùng 3×3. Khử trùng lặp theo `(connectionId, requestId)` để không nhân đôi harvest khi retry. Host xác nhận kết quả cùng revision tăng đơn điệu; client cập nhật delta của tile/pen/wallet trên main thread rồi làm bẩn chunk tương ứng.

Join mới cần snapshot ở revision R rồi nhận delta R+1 trở đi có thứ tự. Tách avatar transform replication khỏi dữ liệu farm; nhân vật có thể interpolate, tiền và cây phải authoritative. Khi host thoát, save hoặc quy định migration rõ ràng. Bổ sung test hai client cùng thu một ô, duplicate/reordered packet, reconnect, join giữa autosave, mất kết nối host và đổi mùa trong lúc offline. Chưa có kiểm chứng LAN/Internet hoặc security cho flow này.

### Town, mở rộng đất và tiền tệ

Đã có inventory, coins, diamonds và medals, nhưng giao dịch demo chỉ bán nông sản lấy coins. Town tương lai là scene additive hoặc portal có merchant/catalog dữ liệu. Host định giá theo catalog của mình và thực hiện inventory debit + wallet credit trong một command. Mua mở rộng cần lưu ownership chunk; command gieo/cày/terraform phải kiểm tra ownership trước khi gọi luật farm. Nhiệm vụ cần transaction ID để phần thưởng không bị nhận lặp sau load/reconnect. Không nên giả lập kim cương/huy chương bằng cách tự cộng mỗi lần bán khi chưa có thiết kế kinh tế.

### Công trình, hàng rào và địa hình nước

`blocked` hiện dùng cho footprint chuồng. Để đặt building/road/fence cần occupancy nhiều lớp và một danh mục placement có footprint/rotation/cost. Road có thể đi qua, fence thì chặn; không nên gom tất cả thành một `blocked` boolean nếu muốn đầy đủ gameplay.

Height hiện theo tile dạng bậc, giới hạn -3 đến 8 m, bước đi tối đa 0,75 m. Production terrain cần heightmap chunk, cập nhật mesh/collider khu vực thay đổi, xử lý tiếp giáp chunk và tài sản đặt trên đất. Ao/sông cần water volume/mesh, giới hạn đường đi và lớp occupancy riêng. HDRP Water System/SSR hoặc planar reflection chỉ giải quyết trình bày nước; không tự tạo luật canh tác hay lưu hình dạng sông.

### Photorealistic

`FarmVisualLibrary` là điểm gắn asset thật: đất PBR ướt/khô, cây ba stage, người/máy cày/bò. Chưa có asset thật hoặc animation state machine. Cần thêm bộ mesh/texture người thật, skeleton/retarget/IK, âm thanh, cây ăn quả/tre/hoa anh đào, công trình lão hóa, foliage LOD và simulation thú. Không dùng ảnh/logo/asset của Farm Together 2; tự phát triển nội dung và nhận diện riêng.

Thiết kế ánh sáng hiện có global HDRP Volume với fixed exposure, GradientSky, Fog và sun ngày đêm. Production cần kiểm tra exposure thực tế, sky GI update, reflection probes, contact shadows, water, volumetric budget, snow accumulation shader và các quality tiers. Particle thời tiết hiện là biểu diễn minh họa và không thay đổi mô phỏng.

### Scale và production

Lưới lớn cần streaming chunk, lưu delta/journal hoặc binary blob có version và snapshot bất biến serialize ở worker. Không được đưa `JsonUtility`/API Unity hoặc state đang mutate tùy tiện sang background thread. BFS demo có thể thay bằng A* và route cache với revision invalidation. Instancing hiện chia chunk cho culling nhóm, chưa có per-instance GPU culling hoặc LOD. Đánh giá bằng Unity Profiler/Frame Debugger trên máy mục tiêu trước khi cam kết FPS hoặc số ô.

Tiêu chí release phải bao gồm Unity build thật, lifecycle save trên platform đích, migration save, hai client cạnh tranh cùng harvest, chống mất/thừa tiền, hình ảnh ngày/đêm và từng mùa, build shader variants và playtest điều khiển. Các mục này chưa hoàn thành chỉ từ test lõi .NET.
# Luồng điều khiển và menu bổ sung

`FarmBootstrap` tạo `FarmGameFlow` sau HUD/camera. Flow quản lý các trạng thái MainMenu, Playing, Pointer, Paused, Settings và nguồn mở Settings để quay lại đúng màn hình. Chỉ Playing nhận input nhân vật; Alt/Tab chuyển Pointer, Esc đóng catalog trước rồi mới mở menu. Cursor mở khóa khi mất focus; có khoảng chờ ngắn khi khóa lại để bỏ delta chuột do warp.

`FarmSettings` tải/kiểm tra JSON, áp dụng FPS/vSync, camera frame settings HDRP, shadow resolution/distance và volume SSAO/Bloom. Khoảng nhìn nội suy khi đổi preset. File cài đặt ghi qua tệp tạm và thay thế nguyên tử. `FarmAudio` có hai AudioSource độc lập; volume thực bằng Master nhân Music hoặc SFX. Các clip hiện được tổng hợp tại runtime.

Không dùng Time.timeScale để đóng băng tăng trưởng. Driver dừng thao tác/auto khi ở menu; SeasonTimeManager và các deadline UTC tiếp tục chạy. HUD bị tắt cả GameObject khi ẩn; raycaster chỉ mở ở chế độ con trỏ. EventSystem không xử lý Submit/Navigation khi điều khiển nhân vật, tránh Space kích hoạt nhầm nút đã chọn.

Native QA flag `-farmVerifyFlow` kiểm tra callback UI, chuyển trạng thái, bốn mức HDRP, FPS, bus âm thanh và file cài đặt trong save riêng. Không thay cho human playtest chuột/bàn phím hoặc đo hiệu năng.

