# SalonHair_New

Ung dung ASP.NET Core MVC cho salon toc, co them man hinh `AI tu van kieu toc` de:

- mo webcam va nhan dien dang mat realtime
- tai anh chan dung de phan tich
- luu feedback local de cai thien model
- export/import dataset giua cac may
- gan nhan hang loat tu ca thu muc anh
- train runtime model tu dataset local

## Cong nghe chinh

- .NET 10 (`net10.0`)
- ASP.NET Core MVC
- Entity Framework Core + SQL Server / LocalDB
- MediaPipe Face Landmarker (chay local trong `wwwroot/vendor/mediapipe`)

## Yeu cau moi truong

- Visual Studio 2022/2026 hoac `dotnet SDK 10`
- SQL Server hoac `MSSQLLocalDB`
- Trinh duyet Edge / Chrome / Coc Coc de dung webcam

## Cach chay du an

1. Restore va build:

```powershell
dotnet restore
dotnet build
```

2. Chay app:

```powershell
dotnet run
```

3. Mac dinh profile local:

- HTTP: [http://localhost:5275](http://localhost:5275)
- HTTPS: [https://localhost:7272](https://localhost:7272)

Trong moi truong `Development`, app dang uu tien LocalDB tai `appsettings.Development.json`.

## Tinh nang AI tu van kieu toc

Route chinh:

- [https://localhost:7272/AiSuggest](https://localhost:7272/AiSuggest)
- [http://localhost:5275/AiSuggest](http://localhost:5275/AiSuggest)

Nguoi dung co the:

- `Mo webcam truc tiep`
- `Hoac tai anh`
- nhan goi y kieu toc theo dang mat
- luu feedback `Ket qua dung` hoac `Sua thanh...`
- `Export dataset`
- `Import dataset`
- `Train AI tu dataset`
- `Chon thu muc anh` de gan nhan hang loat

## Gan nhan hang loat

Man hinh batch labeling nam ngay trong trang `AiSuggest`.

Cach dung:

1. Bam `Chon thu muc anh`
2. Chon folder chua anh chan dung
3. App se mo tung anh va tu phan tich khuon mat
4. Bam:
   - `Dung nhan nay & tiep`
   - hoac mot trong cac nut sua nhan `Tron / Vuong / Trai xoan / Dai & tiep`
   - hoac `Bo qua anh nay`
5. Sau khi du mau, bam `Train AI tu dataset`

Luu y:

- Batch mode hien phu hop nhat voi anh 1 nguoi, mat chinh dien, anh sang ro
- Neu anh khong co mat hoac co nhieu hon 1 khuon mat, app se bo qua

## Dataset local va train model

Feedback duoc luu local theo may trong:

`%LOCALAPPDATA%\SalonHair\AiFeedback`

Thu muc nay gom:

- `samples/`: metadata va feature cua tung mau
- `snapshots/`: anh snapshot da luu
- `models/face-shape-runtime-model.json`: model runtime sau khi train

Neu muon chuyen dataset sang may khac:

1. Bam `Export dataset`
2. Mang file `.zip` sang may khac
3. Bam `Import dataset`
4. Bam `Train AI tu dataset` neu can train lai

Co the doi duong dan dataset bang config:

```json
{
  "AiFeedbackStorage": {
    "RootPath": "D:\\SalonHair\\AiFeedback"
  }
}
```

## Cac file chinh lien quan den AI

- `Program.cs`: dang ky service va khoi tao app
- `Controllers/AiSuggestController.cs`: API phan tich, feedback, export/import, train model
- `Views/AiSuggest/Index.cshtml`: giao dien AI
- `wwwroot/js/ai-suggest.js`: logic webcam, upload anh, batch labeling, feedback, train model
- `Services/AiFeedbackLocalStore.cs`: luu dataset local
- `Services/AiFeedbackModelTrainer.cs`: train runtime model tu dataset

## Cach sua anh goi y kieu toc

Anh goi y hien dang duoc map trong:

- `Controllers/AiSuggestController.cs` trong ham `GetCuratedImageUrl(...)`
- `Controllers/AiSuggestController.cs` trong ham `GetFallbackSuggestions(...)`

Neu muon sua:

- doi URL anh trong `GetCuratedImageUrl(...)`
- hoac doi anh fallback trong `GetFallbackSuggestions(...)`

Khuyen nghi de on dinh hon:

- luu anh vao `wwwroot/images/hairstyles`
- sau do dung duong dan local nhu `/images/hairstyles/mullet.jpg`

## Ghi chu cho thanh vien trong nhom

- Neu bi loi cong `5275 already in use`, hay tat instance `SalonHair.exe` cu roi chay lai
- Webcam thuong hoat dong tot nhat tren `localhost` hoac `HTTPS`
- Neu database khong khoi tao duoc, app van tiep tuc chay va man AI van dung duoc bo goi y fallback
- Khong nen commit `bin/`, `obj/`, `.vs/` len repo khi chi thay doi source
