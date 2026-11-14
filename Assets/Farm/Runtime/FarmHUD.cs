using System;
using System.Collections.Generic;
using NextGenFarm.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace NextGenFarm
{
    public enum FarmIcon { Leaf, Coin, Diamond, Medal, Plow, Seed, Water, Harvest, Tractor, Auto, Shop, Sun, Corn, Carrot, Wheat, Photo, Save }
    public static class FarmUIArt
    {
        private static readonly Dictionary<FarmIcon,Sprite> icons=new Dictionary<FarmIcon,Sprite>();
        private static Sprite rounded;
        public static Sprite Rounded
        {
            get
            {
                if(rounded!=null)return rounded;
                const int n=64;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Bilinear;
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    Vector2 q=new Vector2(Mathf.Abs(x-31.5f)-15.5f,Mathf.Abs(y-31.5f)-15.5f);
                    float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-15;
                    tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-d)));
                }
                tex.Apply();rounded=Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(22,22,22,22));return rounded;
            }
        }
        public static Sprite Icon(FarmIcon type)
        {
            if(icons.TryGetValue(type,out var s))return s;
            const int n=96;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);var pixels=new Color[n*n];
            Color green=new Color(.29f,.51f,.15f),gold=new Color(.95f,.72f,.22f),brown=new Color(.43f,.28f,.14f),blue=new Color(.16f,.65f,.79f),white=new Color(1,.98f,.86f);
            Action<float,float,float,Color> circle=(cx,cy,r,c)=>{for(int y=Mathf.Max(0,(int)(cy-r-1));y<Mathf.Min(n,cy+r+2);y++)for(int x=Mathf.Max(0,(int)(cx-r-1));x<Mathf.Min(n,cx+r+2);x++){float a=Mathf.Clamp01(r+.5f-Vector2.Distance(new Vector2(x,y),new Vector2(cx,cy)));if(a>0)pixels[y*n+x]=Color.Lerp(pixels[y*n+x],c,a);}};
            Action<Vector2,Vector2,float,Color> line=(a,b,w,c)=>{int steps=Mathf.CeilToInt(Vector2.Distance(a,b)*1.5f);for(int i=0;i<=steps;i++){var p=Vector2.Lerp(a,b,i/(float)Mathf.Max(1,steps));circle(p.x,p.y,w,c);}};
            Action<float,float,float,float,Color> rect=(x,y,w,h,c)=>{for(int py=Mathf.Max(0,(int)y);py<Mathf.Min(n,y+h);py++)for(int px=Mathf.Max(0,(int)x);px<Mathf.Min(n,x+w);px++)pixels[py*n+px]=c;};
            if(type==FarmIcon.Coin||type==FarmIcon.Medal||type==FarmIcon.Sun)
            {circle(48,48,31,brown);circle(48,51,29,gold);circle(48,51,22,new Color(1,.84f,.37f));if(type==FarmIcon.Medal){line(new Vector2(35,23),new Vector2(26,7),7,blue);line(new Vector2(61,23),new Vector2(70,7),7,blue);}else{rect(43,35,10,32,white);rect(35,44,27,8,white);}}
            else if(type==FarmIcon.Diamond)
            {for(int y=15;y<82;y++)for(int x=12;x<84;x++)if(Mathf.Abs(x-48)/35f+Mathf.Abs(y-49)/34f<1)pixels[y*n+x]=x<48?new Color(.38f,.83f,.85f):blue;line(new Vector2(31,64),new Vector2(48,78),2,white);}
            else if(type==FarmIcon.Water)
            {for(int y=24;y<77;y++){float w=y>48?(77-y)*.65f:Mathf.Sqrt(Mathf.Max(0,25*25-(y-46)*(y-46)));for(int x=(int)(48-w);x<48+w;x++)pixels[y*n+x]=blue;}circle(40,38,6,white);}
            else if(type==FarmIcon.Tractor||type==FarmIcon.Auto)
            {rect(23,34,52,25,new Color(.86f,.38f,.2f));rect(30,59,26,16,blue);rect(34,63,18,9,white);circle(29,29,17,brown);circle(73,29,12,brown);circle(29,29,9,gold);circle(73,29,6,gold);rect(67,59,5,16,brown);}
            else if(type==FarmIcon.Plow||type==FarmIcon.Harvest)
            {line(new Vector2(24,17),new Vector2(65,72),5,brown);if(type==FarmIcon.Plow){line(new Vector2(48,71),new Vector2(79,57),9,blue);}else{for(int i=0;i<20;i++){float a=(20+i*7)*Mathf.Deg2Rad;circle(50+Mathf.Cos(a)*27,56+Mathf.Sin(a)*27,5,gold);}}}
            else if(type==FarmIcon.Shop)
            {rect(20,20,56,41,brown);rect(27,25,42,30,white);for(int i=0;i<5;i++)rect(16+i*13,57,13,18,i%2==0?green:gold);rect(45,24,13,25,green);}
            else if(type==FarmIcon.Photo||type==FarmIcon.Save)
            {rect(17,23,62,46,brown);rect(28,67,23,10,brown);circle(48,46,20,blue);circle(48,46,12,white);circle(53,51,5,blue);}
            else if(type==FarmIcon.Corn||type==FarmIcon.Wheat)
            {for(int row=0;row<7;row++)for(int col=0;col<3;col++)circle(37+col*10,28+row*7,5,gold);line(new Vector2(22,19),new Vector2(37,43),7,green);line(new Vector2(75,20),new Vector2(58,47),7,green);}
            else if(type==FarmIcon.Carrot)
            {for(int y=15;y<61;y++){float w=(y-10)*.28f;for(int x=(int)(48-w);x<48+w;x++)pixels[y*n+x]=new Color(.96f,.46f,.13f);}line(new Vector2(48,59),new Vector2(25,79),5,green);line(new Vector2(48,60),new Vector2(51,85),5,green);line(new Vector2(49,60),new Vector2(73,78),5,green);}
            else
            {for(int i=0;i<23;i++){float t=i/22f;circle(27+t*40,25+t*48,Mathf.Sin(t*Mathf.PI)*17,green);}line(new Vector2(23,18),new Vector2(67,73),2,white);}
            tex.SetPixels(pixels);tex.Apply();s=Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f));icons.Add(type,s);return s;
        }
    }

    public sealed class FarmHUD : MonoBehaviour
    {
        private FarmBootstrap app;
        private Canvas canvas;
        private RectTransform root,modal,modalBlocker;
        private Font font,bold;
        private Text coins,diamonds,medals,season,seasonTime,contextTitle,contextInfo,toast,questWater,questHarvest;
        private Image contextProgress,minimap;
        private RectTransform playerDot;
        private Texture2D mapTexture;
        private readonly List<Image> toolFaces=new List<Image>();
        private readonly List<RectTransform> waterMarkers=new List<RectTransform>();
        private readonly List<Text> toolCaptions=new List<Text>();
        private float nextTick,toastUntil;
        private readonly Color cream=new Color(.985f,.955f,.86f),ink=new Color(.18f,.28f,.20f),green=new Color(.28f,.46f,.19f),gold=new Color(.82f,.67f,.36f);
        public void Initialize(FarmBootstrap bootstrap)
        {
            app=bootstrap;font=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial"},20);bold=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI Semibold","Arial"},24);
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(bold==null)bold=font;
            var go=new GameObject("Đồng Nội · HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            root=go.GetComponent<RectTransform>();
            if(FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("Farm UI input",typeof(EventSystem));events.transform.SetParent(transform,false);
#if ENABLE_LEGACY_INPUT_MANAGER
                events.AddComponent<StandaloneInputModule>();
#elif ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#endif
            }
            for(int i=0;i<12;i++)
            {
                var m=Panel(root,"Water marker",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,48),new Color(1,.91f,.56f));
                Icon(m,FarmIcon.Water,new Vector2(4,5),new Vector2(34,37));m.gameObject.SetActive(false);waterMarkers.Add(m);
            }
            Header();Dock();Context();Quests();MiniMap();
            toast=Label(root,"",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-116),new Vector2(600,38),22,Color.white,TextAnchor.MiddleCenter,true);
            toast.gameObject.AddComponent<Shadow>().effectColor=new Color(0,0,0,.6f);
            app.Grid.ActionApplied+=OnAction;
            ShowToast("Chào mừng đến Đồng Nội · Tưới ngô để cây lớn nhanh hơn");
        }
        private RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        private Image Image(RectTransform r,Color color)
        {var image=r.gameObject.AddComponent<Image>();image.sprite=FarmUIArt.Rounded;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;image.raycastTarget=false;return image;}
        private RectTransform Panel(Transform p,string name,Vector2 a,Vector2 pv,Vector2 pos,Vector2 size,Color color)
        {var r=Rect(p,name,a,pv,pos,size);var im=Image(r,color);var shadow=im.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.08f,.16f,.10f,.19f);shadow.effectDistance=new Vector2(0,-4);return r;}
        private Text Label(Transform p,string value,Vector2 a,Vector2 pv,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor alignment=TextAnchor.MiddleLeft,bool strong=false)
        {var r=Rect(p,value,a,pv,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=strong?bold:font;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        private void Icon(Transform p,FarmIcon icon,Vector2 pos,Vector2 size)
        {var r=Rect(p,icon.ToString(),Vector2.zero,Vector2.zero,pos,size);var im=r.gameObject.AddComponent<Image>();im.sprite=FarmUIArt.Icon(icon);im.raycastTarget=false;}
        private Button Button(Transform p,string title,Vector2 a,Vector2 pv,Vector2 pos,Vector2 size,Action action,Color? color=null)
        {var r=Panel(p,title,a,pv,pos,size,color??cream);var im=r.GetComponent<Image>();im.raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;var colors=b.colors;colors.highlightedColor=new Color(1,.96f,.77f);colors.pressedColor=new Color(.8f,.88f,.64f);b.colors=colors;b.onClick.AddListener(()=>action());if(!string.IsNullOrEmpty(title))Label(r,title,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(12,4),18,ink,TextAnchor.MiddleCenter,true);return b;}
        private void Header()
        {
            var brand=Panel(root,"Farm identity",new Vector2(0,1),new Vector2(0,1),new Vector2(26,-24),new Vector2(340,80),new Color(.12f,.28f,.22f,.93f));
            Icon(brand,FarmIcon.Leaf,new Vector2(14,13),new Vector2(54,54));
            Label(brand,"ĐỒNG NỘI",Vector2.zero,Vector2.zero,new Vector2(82,32),new Vector2(240,35),27,cream,TextAnchor.MiddleLeft,true);
            Label(brand,"Nông trại bên thung lũng",Vector2.zero,Vector2.zero,new Vector2(83,13),new Vector2(230,24),15,new Color(.78f,.86f,.66f));
            coins=Resource(FarmIcon.Coin,400);diamonds=Resource(FarmIcon.Diamond,590);medals=Resource(FarmIcon.Medal,755);
            var weather=Panel(root,"Season",new Vector2(1,1),new Vector2(1,1),new Vector2(-26,-24),new Vector2(266,80),cream);
            Icon(weather,FarmIcon.Sun,new Vector2(16,16),new Vector2(48,48));
            season=Label(weather,"MÙA HẠ",Vector2.zero,Vector2.zero,new Vector2(80,40),new Vector2(175,27),21,ink,TextAnchor.MiddleLeft,true);
            seasonTime=Label(weather,"Ngày yên bình",Vector2.zero,Vector2.zero,new Vector2(80,15),new Vector2(175,24),16,green);
        }
        private Text Resource(FarmIcon icon,float x)
        {var r=Panel(root,icon.ToString(),new Vector2(0,1),new Vector2(0,1),new Vector2(x,-32),new Vector2(icon==FarmIcon.Coin?172:148,60),cream);Icon(r,icon,new Vector2(12,10),new Vector2(40,40));return Label(r,"0",Vector2.zero,Vector2.zero,new Vector2(60,10),new Vector2(100,40),24,ink,TextAnchor.MiddleLeft,true);}
        private void Dock()
        {
            var dock=Panel(root,"Tools",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,28),new Vector2(768,116),new Color(.13f,.27f,.21f,.95f));
            var icons=new[]{FarmIcon.Plow,FarmIcon.Seed,FarmIcon.Water,FarmIcon.Harvest,FarmIcon.Tractor,FarmIcon.Auto,FarmIcon.Shop};
            string[] names={"Cày đất","Gieo hạt","Tưới cây","Thu hoạch","Máy cày","Tự lái","Cửa hàng"};string[] keys={"1","2","3","4","T","R","B"};
            for(int i=0;i<7;i++)
            {
                int index=i;var b=Button(dock,"",Vector2.zero,Vector2.zero,new Vector2(14+i*106,13),new Vector2(98,90),()=>Tool(index),i==2?new Color(.94f,.85f,.55f):cream);
                Icon(b.transform,icons[i],new Vector2(29,36),new Vector2(40,40));
                toolCaptions.Add(Label(b.transform,names[i],Vector2.zero,Vector2.zero,new Vector2(0,8),new Vector2(98,22),14,ink,TextAnchor.MiddleCenter,true));
                Label(b.transform,keys[i],new Vector2(1,1),new Vector2(1,1),new Vector2(-6,-4),new Vector2(18,18),12,green,TextAnchor.MiddleCenter,true);
                toolFaces.Add(b.GetComponent<Image>());
            }
            Label(root,"WASD  Di chuyển     •     Cuộn chuột  Khoảng cách     •     Giữ chuột phải  Xoay góc nhìn",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,5),new Vector2(850,20),14,Color.white,TextAnchor.MiddleCenter);
        }
        private void Tool(int index)
        {if(index<4){app.Driver.SelectedAction=(FarmAction)index;ShowToast(new[]{"Chọn đất trống rồi nhấn Space để cày","Chọn giống trong cửa hàng, nhấn Space để gieo","Tưới một lần mỗi vụ để giảm thời gian chờ","Cây chín luôn chờ bạn quay lại"}[index]);}else if(index==4)app.Driver.SetMounted(!app.Driver.Mounted);else if(index==5)app.Driver.ToggleAuto();else OpenShop(0);}
        private void Context()
        {
            var card=Panel(root,"Crop context",new Vector2(0,0),Vector2.zero,new Vector2(26,28),new Vector2(322,174),cream);
            Icon(card,FarmIcon.Corn,new Vector2(15,93),new Vector2(54,54));
            contextTitle=Label(card,"Ngô",Vector2.zero,Vector2.zero,new Vector2(82,118),new Vector2(224,30),23,ink,TextAnchor.MiddleLeft,true);
            contextInfo=Label(card,"Đang sinh trưởng",Vector2.zero,Vector2.zero,new Vector2(82,87),new Vector2(224,30),15,green);
            var bar=Panel(card,"Growth",Vector2.zero,Vector2.zero,new Vector2(18,70),new Vector2(286,7),new Color(.83f,.86f,.7f));
            contextProgress=Image(Rect(bar,"Fill",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(0,7)),green);
            Button(card,"SPACE   Thao tác",Vector2.zero,Vector2.zero,new Vector2(16,18),new Vector2(172,38),()=>app.Driver.UseTool(),new Color(.86f,.89f,.69f));
            Button(card,"Giống cây",Vector2.zero,Vector2.zero,new Vector2(196,18),new Vector2(110,38),()=>OpenShop(0));
        }
        private void Quests()
        {
            var card=Panel(root,"Farm requests",new Vector2(1,1),new Vector2(1,1),new Vector2(-26,-125),new Vector2(286,183),cream);
            Label(card,"MỘT NGÀY Ở ĐỒNG NỘI",new Vector2(0,1),new Vector2(0,1),new Vector2(18,-12),new Vector2(252,28),16,ink,TextAnchor.MiddleLeft,true);
            questWater=Label(card,"Tưới cây   0 / 9",Vector2.zero,Vector2.zero,new Vector2(18,91),new Vector2(250,29),17,green);
            questHarvest=Label(card,"Thu hoạch   0 / 9",Vector2.zero,Vector2.zero,new Vector2(18,58),new Vector2(250,29),17,green);
            Button(card,"Nhận quà chăm sóc",Vector2.zero,Vector2.zero,new Vector2(17,13),new Vector2(252,35),()=>{bool got=app.Grid.World.ClaimCareReward(true)|app.Grid.World.ClaimCareReward(false);ShowToast(got?"Đã nhận phần thưởng chăm sóc nông trại":"Hoàn thành 9 ô tưới hoặc 9 ô thu hoạch để nhận quà");if(got)app.Saves.Save();},new Color(.88f,.9f,.74f));
            Button(root,"Ảnh  F12",new Vector2(1,1),new Vector2(1,1),new Vector2(-26,-322),new Vector2(135,38),()=>app.TakePhoto());
            Button(root,"Lưu",new Vector2(1,1),new Vector2(1,1),new Vector2(-171,-322),new Vector2(100,38),()=>{app.Saves.Save();ShowToast("Nông trại đã được lưu");});
        }
        private void MiniMap()
        {
            var card=Panel(root,"Farm map",new Vector2(1,0),new Vector2(1,0),new Vector2(-26,28),new Vector2(232,218),cream);
            Label(card,"THUNG LŨNG XANH",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-8),new Vector2(220,26),14,ink,TextAnchor.MiddleCenter,true);
            var r=Rect(card,"Map",Vector2.zero,Vector2.zero,new Vector2(17,15),new Vector2(198,168));mapTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);mapTexture.filterMode=FilterMode.Point;
            minimap=r.gameObject.AddComponent<Image>();minimap.sprite=Sprite.Create(mapTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));minimap.raycastTarget=false;
            playerDot=Rect(r,"You",Vector2.zero,new Vector2(.5f,.5f),Vector2.zero,new Vector2(10,10));Image(playerDot,new Color(1,.9f,.5f));
        }
        public void ShowToast(string text){if(toast!=null){toast.text=text;toastUntil=Time.unscaledTime+4.5f;}}
        public void SetVisible(bool visible) { if(canvas!=null)canvas.enabled=visible; }
        private void OnAction(Vector2Int tile,FarmAction action,int count)
        {ShowToast(action==FarmAction.Harvest?"Thu hoạch thành công  ·  "+count+" ô":action==FarmAction.Water?"Đã tưới "+count+" ô  ·  Cây lớn nhanh hơn":action==FarmAction.Sow?"Đã gieo "+count+" ô "+CropName(app.Driver.SelectedCrop):"Đã cày "+count+" ô đất");}
        public static string CropName(string id)=>id=="corn"?"Ngô":id=="wheat"?"Lúa mì":id=="carrot"?"Cà rốt":id;
        private void Update()
        {
            if(app==null||app.Grid==null)return;
            if(Time.unscaledTime>toastUntil)toast.text="";
            for(int i=0;i<toolFaces.Count;i++) toolFaces[i].color=i<4&&i==(int)app.Driver.SelectedAction?new Color(.95f,.85f,.48f):i==4&&app.Driver.Mounted?new Color(.84f,.91f,.65f):i==5&&app.Driver.Automatic?new Color(.67f,.87f,.78f):cream;
            if(Time.unscaledTime<nextTick)return;nextTick=Time.unscaledTime+.15f;
            var w=app.Grid.World;coins.text=w.Wallet.coins.ToString("N0");diamonds.text=w.Wallet.diamonds.ToString("N0");medals.text=w.Wallet.medals.ToString("N0");
            string[] names={"MÙA XUÂN","MÙA HẠ","MÙA THU","MÙA ĐÔNG"};season.text=names[(int)app.Clock.CurrentSeason];
            var remaining=TimeSpan.FromSeconds(app.Clock.SecondsRemaining);seasonTime.text=remaining.ToString(@"mm\:ss")+"  ·  "+(app.Clock.DayFraction>.25f&&app.Clock.DayFraction<.75f?"Ban ngày":"Ban đêm");
            var c=app.Driver.TargetCell;var tile=w.Get(c.x,c.y);
            contextTitle.text=tile!=null&&tile.HasCrop?CropName(tile.cropId):tile!=null&&tile.road?"Đường đá":"Đất nông trại";
            contextInfo.text=tile==null?"Ngoài ranh giới nông trại":tile.HasCrop?(tile.Ready(app.Clock.Now)?"Đã chín · Sẵn sàng thu hoạch":Math.Max(0,tile.readyUtc-app.Clock.Now).ToString("F0")+" giây  ·  "+(tile.wateredThisCycle?"Đã tưới":"Cần tưới")):(tile.tilled?"Sẵn sàng gieo hạt":"Chọn công cụ để bắt đầu");
            contextProgress.rectTransform.sizeDelta=new Vector2(286*(tile!=null?tile.Growth(app.Clock.Now):0),7);
            questWater.text="Tưới cây    "+Mathf.Min(9,w.Wallet.watered)+" / 9"+(w.Wallet.waterRewardClaimed?"  ✓":"");
            questHarvest.text="Thu hoạch    "+Mathf.Min(9,w.Wallet.harvested)+" / 9"+(w.Wallet.harvestRewardClaimed?"  ✓":"");
            Markers();Map();
        }
        private void Markers()
        {
            int used=0;var player=app.Grid.WorldToCell(app.Driver.transform.position);var camera=Camera.main;
            for(int z=player.y-1;z<=player.y+6&&used<waterMarkers.Count;z++)for(int x=player.x-4;x<=player.x+4&&used<waterMarkers.Count;x++)
            {
                var tile=app.Grid.World.Get(x,z);if((x+z)%2!=0||tile==null||!tile.HasCrop||tile.wateredThisCycle||tile.Ready(app.Clock.Now))continue;
                Vector3 p=camera.WorldToScreenPoint(app.Grid.CellCenter(x,z)+Vector3.up*(tile.cropId=="corn"?2.05f:1.15f));
                if(p.z<0||p.x<20||p.x>Screen.width-20||p.y<130||p.y>Screen.height-60)continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,new Vector2(p.x,p.y),null,out var local);
                var marker=waterMarkers[used++];marker.gameObject.SetActive(true);marker.anchoredPosition=local;
            }
            for(int i=used;i<waterMarkers.Count;i++)waterMarkers[i].gameObject.SetActive(false);
        }
        private void Map()
        {
            var w=app.Grid.World;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {var t=w.Get(x*w.Width/64,y*w.Depth/64);mapTexture.SetPixel(x,y,t.blocked?new Color(.7f,.48f,.3f):t.road?new Color(.85f,.8f,.62f):t.HasCrop?t.cropId=="corn"?new Color(.38f,.64f,.2f):t.cropId=="wheat"?new Color(.83f,.65f,.25f):new Color(.3f,.53f,.22f):new Color(.55f,.67f,.35f));}
            mapTexture.Apply();var p=app.Grid.WorldToCell(app.Driver.transform.position);playerDot.anchoredPosition=new Vector2((p.x+.5f)/w.Width*198,(p.y+.5f)/w.Depth*168);
        }
        public void OpenShop(int page)
        {
            CloseShop();
            modalBlocker=Rect(root,"Catalog backdrop",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,2400));
            Image(modalBlocker,new Color(.04f,.12f,.09f,.55f)).raycastTarget=true;
            modal=Panel(modalBlocker,"Catalog",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(960,600),cream);
            modal.GetComponent<Image>().raycastTarget=true;
            Label(modal,"CỬA HÀNG NÔNG TRẠI",new Vector2(0,1),new Vector2(0,1),new Vector2(35,-26),new Vector2(800,42),30,ink,TextAnchor.MiddleLeft,true);
            Button(modal,"×",new Vector2(1,1),new Vector2(1,1),new Vector2(-20,-20),new Vector2(44,44),CloseShop);
            string[] tabs={"Hạt giống","Kho nông sản","Vật nuôi"};for(int i=0;i<3;i++){int index=i;Button(modal,tabs[i],new Vector2(0,1),new Vector2(0,1),new Vector2(36+i*230,-90),new Vector2(216,45),()=>OpenShop(index),i==page?new Color(.85f,.89f,.66f):new Color(.94f,.91f,.8f));}
            if(page==0)
            {
                for(int i=0;i<app.crops.Length;i++)
                {
                    var crop=app.crops[i];var card=Panel(modal,crop.id,new Vector2(0,1),new Vector2(0,1),new Vector2(36+i*295,-160),new Vector2(274,362),Color.white);
                    Icon(card,crop.id=="corn"?FarmIcon.Corn:crop.id=="wheat"?FarmIcon.Wheat:FarmIcon.Carrot,new Vector2(83,214),new Vector2(108,108));
                    Label(card,CropName(crop.id),Vector2.zero,Vector2.zero,new Vector2(22,171),new Vector2(230,38),27,ink,TextAnchor.MiddleCenter,true);
                    Label(card,crop.seedCost+" xu / ô   ·   "+Mathf.RoundToInt((float)crop.growSeconds/60)+" phút",Vector2.zero,Vector2.zero,new Vector2(22,129),new Vector2(230,30),18,green,TextAnchor.MiddleCenter);
                    Label(card,"Tưới giảm "+crop.waterReductionSeconds+" giây",Vector2.zero,Vector2.zero,new Vector2(22,96),new Vector2(230,30),16,green,TextAnchor.MiddleCenter);
                    Button(card,"Chọn giống",Vector2.zero,Vector2.zero,new Vector2(22,25),new Vector2(230,48),()=>{app.Driver.SelectedCrop=crop.id;app.Driver.SelectedAction=FarmAction.Sow;CloseShop();ShowToast("Đã chọn "+CropName(crop.id)+" · Nhấn Space để gieo");},new Color(.84f,.9f,.65f));
                }
            }
            else if(page==1)
            {
                int row=0;foreach(var entry in app.Grid.World.Wallet.inventory){Label(modal,CropName(entry.id)+"                         "+entry.count, new Vector2(0,1),new Vector2(0,1),new Vector2(48,-172-row*42),new Vector2(820,36),22,ink);row++;}
                if(row==0)Label(modal,"Kho đang trống. Thu hoạch cây chín để có nông sản nhé!",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(800,80),24,green,TextAnchor.MiddleCenter);
                Button(modal,"Bán toàn bộ nông sản",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,35),new Vector2(340,50),()=>{app.SellAll();OpenShop(1);},new Color(.84f,.9f,.65f));
            }
            else
            {
                Label(modal,"Chuồng bò · Sữa tươi mỗi đợt cho ăn",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,40),new Vector2(800,70),26,green,TextAnchor.MiddleCenter,true);
                Button(modal,"Cho ăn",new Vector2(.5f,.5f),new Vector2(1,.5f),new Vector2(-15,-60),new Vector2(250,55),()=>{int n=app.Animals.FeedAll();ShowToast(n>0?"Đã cho vật nuôi ăn":"Chuồng đã có thức ăn hoặc chưa đủ xu");});
                Button(modal,"Thu sản phẩm",new Vector2(.5f,.5f),new Vector2(0,.5f),new Vector2(15,-60),new Vector2(250,55),()=>{int n=app.Animals.HarvestAll();ShowToast(n>0?"Đã thu sữa vào kho":"Sản phẩm chưa sẵn sàng");});
            }
        }
        private void CloseShop(){if(modal!=null)Destroy(modal.gameObject);if(modalBlocker!=null)Destroy(modalBlocker.gameObject);modal=null;modalBlocker=null;}
        private void OnDestroy(){if(app!=null&&app.Grid!=null)app.Grid.ActionApplied-=OnAction;if(mapTexture!=null)Destroy(mapTexture);if(font!=null)Destroy(font);if(bold!=null&&bold!=font)Destroy(bold);}
    }
}
