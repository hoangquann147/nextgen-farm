# NextGen Farm — mã nguồn Unity C# / HDRP

Project hiện dùng **Unity 6000.0.62f1 / HDRP 17.0.4 / Windows desktop**, có `Assets`, `Packages`, `ProjectSettings`, cấu hình tự động, launcher mở Editor và build Player. Game web cũ đã rời workspace. Xem `README.md` để chạy trực tiếp project; các phần bên dưới mô tả luật và cách tái sử dụng mã.

## Chạy trong Unity

**Với project hiện tại:** nhấp `MO-UNITY.cmd`, chờ import/cấu hình lần đầu hoàn tất rồi nhấn Play. `CHOI-GAME.cmd` dùng bản Windows đã build; nếu thiếu bản build thì tự build trước. Không cần copy mã sang project khác.

**Nếu chỉ muốn tái sử dụng mã trong một project khác:**

1. Tạo project bằng template **High Definition 3D / HDRP** trong Unity Hub. Cài HDRP tương thích với Editor qua Package Manager; chạy HDRP Wizard và giải quyết các mục bắt buộc. Bộ code tham chiếu trực tiếp HDRP; không nhập vào project chỉ có URP/Built-in.
2. Copy **nguyên thư mục `Assets/Farm`** của kit vào `Assets` trong project đó. Không copy `Verification` vào `Assets`: đây là chương trình kiểm thử .NET Framework ngoài Unity.
3. Chờ Unity biên dịch, chọn **NextGen Farm → Create Demo Scene**. Menu tạo thư viện material tham chiếu shader để giảm rủi ro shader bị loại khỏi bản build, mở scene mới và cho chọn nơi lưu scene. Nhấn **Play**.
4. Nếu muốn tự tạo scene: tạo GameObject, gắn `FarmBootstrap`, rồi Play. Cách này cũng tạo camera, ánh sáng, volume HDRP và demo; khi build cần giữ các shader runtime bên dưới.
5. Để xuất Player: thêm scene đã lưu vào Build Profiles, dùng HDRP-compatible Graphics API, đặt **Graphics → Shader Stripping → Instancing Variants = Keep All** cho demo. Giữ `HDRP/Lit` và `HDRP/Unlit` bằng material có tham chiếu trong scene hoặc Always Included Shaders. Kiểm tra Frame Debugger trong bản build thực tế trước khi tối ưu stripping.

Kit tự chọn Input System mới nếu `ENABLE_INPUT_SYSTEM` được bật, nếu không dùng Legacy Input khi `ENABLE_LEGACY_INPUT_MANAGER` được bật. Vì vậy không bắt buộc đổi sang Both. Cần ít nhất một backend input được bật. Không cần khai báo các trục input tên riêng.

## Điều khiển và vòng chơi

| Phím | Hành động |
| --- | --- |
| WASD / mũi tên | Di chuyển theo trục bản đồ |
| Cuộn chuột | Zoom liên tục từ theo sau người chơi đến góc nhìn cao |
| T | Đổi đi bộ / lái máy cày |
| 1, 2, 3, 4 | Chọn cày, gieo, tưới, thu hoạch |
| Space | Dùng công cụ tại ô phía trước; đi bộ 1×1, máy cày 3×3 |
| R | Chạy/dừng Auto-Tractor; WASD hủy tự động |
| B | Đổi giống cây |
| F / G | Cho ăn / thu sản phẩm tất cả chuồng demo |
| V | Bán nông sản trong kho theo bảng giá |
| P | Lưu ngay; đồng thời tự lưu mỗi 30 giây, khi pause và khi thoát bình thường |
| + / - | Nâng/hạ ô phía trước từng 0,25 m |

Thử vòng chơi: cày → gieo cà rốt (120 giây mặc định) → tưới để giảm 30 giây → thu hoạch → V để bán. Dùng R với mỗi công cụ để chạy một lượt toàn bộ bản đồ. Một lượt auto chỉ sử dụng **công cụ đang chọn**; không tự chuyển cây chưa chín sang thao tác khác. Chuyển sang thu hoạch sau khi cây chín rồi chạy R lại. Trợ giúp, tiền và thời gian còn lại hiển thị trên HUD.

Auto-Tractor tạo các tâm vùng 3×3 theo đường rắn, tính đường BFS 4 hướng giữa các tâm, tránh ô chuồng và bước địa hình cao quá 0,75 m. Bỏ qua tâm không tới được; ô bên trong vật cản không phải đất canh tác. Chỉ thao tác khi đã tới tâm; không thao tác từ xa. Không phải hệ vật lý bánh xe hoặc tự đi theo một spline đường do người chơi vẽ.

## Quy tắc mô phỏng

- Một mùa dài đúng **1.020 giây thực**, bốn mùa lặp lại. Mùa gieo cây được cấu hình bằng `seasonMask` (bit 0–3 = Xuân–Hạ–Thu–Đông); cây đã trồng không chết khi đổi mùa.
- Crop lưu `plantedUtc`, `readyUtc`, `wetUntilUtc`, `wateredThisCycle`. So sánh với `Now` thay vì tăng tiến độ bằng `deltaTime`; bật lại game sẽ thấy tiến độ offline ngay, không cần lặp từng giây bỏ lỡ.
- Tưới được hưởng giảm thời gian **một lần/vụ**, kể cả sau khi đất khô. Gieo vào đất còn ướt cũng dùng quyền giảm thời gian đó. Không tưới vẫn chín theo thời gian gốc, không héo và không mất sản lượng.
- Chuồng được cấp một chu kỳ sản xuất sau khi cho ăn. Hết thời gian thì sản phẩm đợi người chơi, không tự mất; không được cộng vô hạn chu kỳ nếu offline lâu mà chưa cho ăn lại. Không cho ăn không làm chết thú.
- Hành động 3×3 xử lý theo hàng, bỏ ô không hợp lệ; nếu thiếu tiền thì chỉ gieo được số ô trả tiền được. Không có giao dịch nguyên khối cả vùng; kết quả trả về số ô đã thay đổi.
- Coins dùng `long`, kho và thu hoạch có kiểm tra tràn số. Diamonds và medals có dữ liệu lưu nhưng chưa có nhiệm vụ/phần thưởng hay cửa hàng tiêu các loại tiền đó.
- Đồng hồ trong phiên dựa trên thời gian monotonic của Unity, khi khởi động dựa vào UTC máy và không thấp hơn mốc save. Đây không phải đồng hồ máy chủ chống gian lận: sửa giờ OS khi offline vẫn có thể đẩy nhanh tiến độ.

## Cấu trúc mã

| File | Trách nhiệm |
| --- | --- |
| `Runtime/Core/FarmModel.cs` | `FarmTile`, định nghĩa cây, ví/kho, luật giao dịch lưới và mùa |
| `Runtime/Core/AnimalModel.cs` | Chuồng, giữ chỗ footprint, cho ăn và thu sản phẩm |
| `Runtime/Core/GridNavigation.cs` | BFS tránh vật cản và tuyến phủ bản đồ 3×3 |
| `Runtime/Core/FarmSnapshot.cs` | Schema v1; xác thực toàn bộ và phục hồi dữ liệu vào world mới |
| `Runtime/Core/FarmSaveStore.cs` | Lưu file tạm, flush, thay nguyên tử và backup |
| `Runtime/FarmGridManager.cs` | Chuyển tọa độ world ↔ lưới và API thao tác Unity |
| `Runtime/TractorController.cs` | Đi bộ, lái, quét 3×3 và tự lái |
| `Runtime/AnimalPenSystem.cs` | Adapter MonoBehaviour cho hệ chuồng |
| `Runtime/SeasonTimeManager.cs` | UTC, mùa, event đổi mùa, chu kỳ ngày đêm 10 phút |
| `Runtime/SaveLoadSystem.cs` | JsonUtility, đường dẫn save, autosave/lifecycle |
| `Runtime/FarmInstanceRenderer.cs` | Cache ma trận theo chunk và GPU instancing |
| `Runtime/FarmVisualLibrary.cs` | Asset cấu hình mesh/material/prefab thật |
| `Runtime/FarmCamera.cs`, `FarmInput.cs` | Camera và hai backend input |
| `Runtime/FarmEnvironment.cs` | Sun, HDRP exposure/sky/fog và mưa/tuyết minh họa |
| `Runtime/FarmBootstrap.cs` | Composition root, scene demo, HUD và bán nông sản |
| `Editor/FarmDemoMenu.cs` | Menu dựng scene và asset tham chiếu shader |
| `Editor/FarmUnityChecks.cs` | Kiểm tra JsonUtility/save IO trong Editor hoặc batchmode |
| `Editor/FarmProjectSetup.cs` | Tự cấu hình project, HDRP, scene và build Windows |
| `Editor/FarmPlayModeChecks.cs` | Chạy vòng chơi, IO và chuyển động auto thật trong Play Mode |

Toàn bộ file C# có trong `CODE_FULL.md` để đọc/copy liên tục. Nên copy cây thư mục `Assets/Farm` để giữ tên file và vị trí `Editor`, thay vì ghép tất cả thành một file.

## Save và khôi phục

Đường dẫn: `Application.persistentDataPath/nextgen-farm-v1.json` (Windows Player thường ở `%USERPROFILE%/AppData/LocalLow/<Company>/<Product>`). Backup: `.json.bak`; file ghi đang dở: `.json.tmp`.

Ghi đồng bộ trên main thread để lấy snapshot nhất quán: serialize → ghi `.tmp` → `Flush(true)` → `File.Replace` và giữ bản cũ `.bak`. Lần đầu dùng `File.Move`. Đây là thiết kế cho filesystem desktop Windows; cần thay storage adapter khi port WebGL hoặc dịch vụ cloud. Không bảo đảm dữ liệu của phiên chưa kịp autosave nếu tiến trình bị kill/mất điện; tốc độ phát triển offline chỉ áp dụng cho các hành động đã được lưu.

Khi tải: kiểm tra version, kích thước, timestamp hữu hạn, số dư, ID định nghĩa, tile, chuồng trùng nhau và kho trùng ID. Chỉ đưa world mới vào game sau khi toàn bộ hợp lệ. Nếu save chính lỗi, thử backup. Nếu cả hai lỗi hoặc thiếu definition ID cũ, dừng khởi tạo và giữ nguyên file để người dùng phục hồi; không tự ghi đè nông trại bằng world trống. Thay đổi nội dung khi phát hành cần giữ ID ổn định và viết migration cho phiên bản schema mới.

Muốn chơi farm mới trong lúc phát triển: thoát Play, sao lưu rồi di chuyển riêng `.json`, `.json.bak`, `.json.tmp` ra khỏi đường dẫn trên. `width/depth` trong Inspector chỉ ảnh hưởng **farm mới**; farm đã lưu khôi phục kích thước cũ.

## Hình ảnh và hiệu năng thực tế

Demo dùng hình khối đơn giản để chạy mà không cần tải asset ngoài. **Chưa phải đồ họa photorealistic**. Đất ướt/khô có màu và độ bóng khác nhau; renderer hỗ trợ thay material HDRP PBR thật. Mưa/tuyết là particle minh họa; chưa có tuyết tích tụ, nước phản chiếu vật lý, cây 3D thật, mô phỏng lông/da hay hoạt ảnh người và thú. Thời tiết hiện chỉ tác động trình bày, không tự tưới online/offline. Công cụ địa hình sửa độ cao từng ô dạng bậc, chưa sửa Unity Terrain liên tục hoặc tạo ao/sông.

Để gắn asset thực: tạo `NextGen Farm/Visual Library`, điền material đất, `CropVisual` theo `cropId` với ba mesh/material giai đoạn, prefab người/máy cày/bò, gán vào Bootstrap. Mesh cây cần pivot ở chân, một submesh; mesh đất giả định cube 1 m có pivot giữa và mặt phẳng trên Y=0,5. Prefab có thể chứa Animator của asset; code hiện không điều khiển locomotion animation. Dùng texture PBR có giấy phép riêng, LOD và cỏ/vegetation renderer chuyên dụng trước khi nhận mục tiêu photorealistic.

Renderer chia chunk 16×16; tối đa 256 instance/batch cho cùng mesh/material. Không tạo một GameObject hoặc MonoBehaviour cho từng tile/crop. Ma trận chỉ dựng lại khi tile thay đổi hoặc đến mốc đổi stage/khô đất; gửi batch bằng `Graphics.RenderMeshInstanced`. HISM là khái niệm của Unreal; ở đây dùng GPU instancing của Unity. Culling theo bounds của nhóm instance, chưa có occlusion/LOD từng cây. Cây mặc định có ba trạng thái hình học rời, không scale liên tục từng frame. Số draw call trong HUD là số lệnh instancing gửi từ CPU, không phải tổng GPU draws sau shadow passes/culling.

BFS cấp phát dữ liệu O(số ô) khi tìm mỗi đoạn đường, không chạy mỗi frame. Save JSON toàn farm là O(số ô) và đồng bộ; farm lớn có thể gây hitch. Giới hạn model là 262.144 ô, không phải tuyên bố đã benchmark map cỡ đó. Chưa đo FPS/GC/render time trong Editor hoặc Player.

API và giới hạn instancing đối chiếu với [Unity RenderMeshInstanced](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html). Cấu hình ánh sáng tham khảo [HDRP Exposure](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition%4017.0/manual/reference-override-exposure.html), [Visual Environment](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition%4017.0/manual/Override-Visual-Environment.html). Input theo [Unity Input System Installation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.13/manual/Installation.html).

## Kiểm chứng

Đã biên dịch lõi C# bằng Roslyn 4.12 và chạy **58 assertion**, gồm 289 kích thước bản đồ trong kiểm tra độ phủ tuyến 3×3. Kiểm tra IO thực chạy trên Windows: JSON roundtrip, thay file, backup, fallback khi lỗi. Test ngoài Unity dùng `JavaScriptSerializer`; đã bổ sung và chạy riêng kiểm tra `JsonUtility` trong Unity.

Unity 6000.0.62f1 và Unity Hub đã được cài ở ổ F. Toàn bộ 20 file C# runtime/Editor đã được biên dịch thật trong Unity. `FARM_PROJECT_CHECKS_PASSED`, `FARM_UNITY_CHECKS_PASSED` và `FARM_PLAYMODE_CHECKS_PASSED` đã xuất hiện trong các lượt chạy có exit code 0. Play Mode kiểm tra cày/gieo/tưới/thu hoạch 3×3, sản lượng, chuồng, save roundtrip, batch instancing và chuyển động auto. Tình trạng build Player và kiểm tra hình ảnh mới nhất xem `Verification/REPORT.vi.md`. Chưa đo FPS bằng Profiler/Frame Debugger và chưa có multiplayer. Compiler Roslyn nằm trong `.tools`, được gitignore và không nằm trong ZIP source.

Chạy lại test lõi trên Windows có Roslyn `csc.exe` và .NET Framework 4.8:

```powershell
./Verification/run.ps1 -Compiler 'C:/path/to/Roslyn/csc.exe'
```

Trong môi trường hiện tại đã có compiler tải cục bộ, chỉ cần `./Verification/run.ps1`. Nếu tải lại, lấy `Microsoft.Net.Compilers.Toolset` 4.12.0 từ [NuGet chính thức](https://www.nuget.org/packages/Microsoft.Net.Compilers.Toolset/4.12.0), giải nén và dùng `tasks/net472/csc.exe`; PowerShell có thể cần unblock file tải theo chính sách của máy.

Sau khi nhập Unity, chạy **NextGen Farm → Verify Unity JSON and Save IO**. Có thể chạy batchmode:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/<installed-version>/Editor/Unity.exe' `
  -batchmode -projectPath 'C:/path/to/HDRPProject' `
  -executeMethod NextGenFarm.Editor.FarmUnityChecks.Run -quit -logFile 'C:/path/to/unity-check.log'
```

Đọc log và tìm `FARM_UNITY_CHECKS_PASSED`, đồng thời kiểm tra exit code và lỗi compile. Sau đó Play Mode: kiểm tra cày/tưới/thu hoạch 1×1 và 3×3, WASD hủy auto, camera, reload sau 2 phút, backup recovery, một lần chuyển mùa 17 phút; kiểm tra Frame Debugger và standalone build. Test cảm giác điều khiển và chất lượng hình ảnh cần chơi trực tiếp.

## Ranh giới so với toàn bộ game được mô tả

Bốn nhóm mã nguồn đã có triển khai thực, không có method `TODO`/`NotImplementedException`. Tuy nhiên **không nên gọi bộ này là bản sao 95% Farm Together 2 hoặc một game AAA hoàn thiện**: chưa có co-op transport/replication, Town Hub đi lại được, nhiệm vụ thưởng kim cương/huy chương, mua mở rộng đất, hệ đặt đường/hàng rào/trang trí cho người chơi, nước vật lý, asset photorealistic hoặc production QA. Nút bán hàng là giao dịch Town ở cấp mô phỏng, không phải khu thị trấn hoàn chỉnh. Cấu trúc và kế hoạch tích hợp các phần còn lại nằm trong `ARCHITECTURE.vi.md`; chúng được ghi rõ là thiết kế, không giả làm code đã chạy.
