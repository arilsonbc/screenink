using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;

namespace PointofixModern {
 enum Tool { Pen, Marker, Arrow, Rectangle, Ellipse, Text, Eraser, Magnifier, Line, Shape, Picker, Ruler, Protractor, Spotlight, Laser, Select }
 partial class Mark {
  public Tool Tool; public Color Color; public float Width; public string Text;
  public ShapeKind Shape=ShapeKind.Triangle;public Color Fill=Color.Transparent;public DashStyle Dash=DashStyle.Solid;public int Alpha=255;public int ArrowEnds=1;public float TextSize=20;public bool Bold;public string FontFamily="Segoe UI";
  public List<PointF> Points = new List<PointF>();
  public void Draw(Graphics g) {
   if(Points.Count==0)return;
   PointF a=Points[0],b=Points[Points.Count-1];
   RectangleF r=Bounds(a,b);
   if(DrawSpecial(g,a,b,r))return;
   if(Tool==Tool.Text){using(var f=new Font(FontFamily,TextSize,Bold?FontStyle.Bold:FontStyle.Regular))using(var brush=new SolidBrush(Color.FromArgb(Alpha,Color)))g.DrawString(Text??"",f,brush,a);return;}
   using(var p=new Pen(Tool==Tool.Marker?Color.FromArgb(Math.Min(Alpha,90),Color):Color.FromArgb(Alpha,Color),Tool==Tool.Marker?Width*5:Width)) {
    p.StartCap=LineCap.Round;p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;p.DashStyle=Dash;
    if(Tool==Tool.Pen||Tool==Tool.Marker){if(Points.Count>1)DrawStroke(g,p);else g.DrawLine(p,a,new PointF(a.X+.1f,a.Y+.1f));}
    if(Tool==Tool.Arrow){using(var cap=new AdjustableArrowCap(5,6,true)){if(ArrowEnds==1||ArrowEnds==3)p.CustomEndCap=cap;if(ArrowEnds==2||ArrowEnds==3)p.CustomStartCap=cap;g.DrawLine(p,a,b);}}
    if(Tool==Tool.Rectangle&&r.Width>0&&r.Height>0){if(Fill.A>0)using(var brush=new SolidBrush(Color.FromArgb(Alpha,Fill)))g.FillRectangle(brush,r);g.DrawRectangle(p,r.X,r.Y,r.Width,r.Height);}
    if(Tool==Tool.Ellipse&&r.Width>0&&r.Height>0){if(Fill.A>0)using(var brush=new SolidBrush(Color.FromArgb(Alpha,Fill)))g.FillEllipse(brush,r);g.DrawEllipse(p,r);}
   }
  }
  void DrawStroke(Graphics g,Pen p){using(var path=new GraphicsPath()){path.AddLines(Points.ToArray());g.DrawPath(p,path);}}
  public static RectangleF Bounds(PointF a,PointF b){return new RectangleF(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));}
  public bool Hit(PointF p){
   if(Tool==Tool.Text){using(var f=new Font("Segoe UI",TextSize))using(var bm=new Bitmap(1,1))using(var g=Graphics.FromImage(bm)){var size=g.MeasureString(Text??"",f);return new RectangleF(Points[0],size).Contains(p);}}
   if(Tool==Tool.Rectangle||Tool==Tool.Ellipse||Tool==Tool.Shape){var r=Bounds(Points[0],Points[Points.Count-1]);r.Inflate(12,12);return r.Contains(p);}
   for(int i=0;i<Points.Count;i++){if(i==0){if(Distance(p,Points[i])<15)return true;}else if(SegmentDistance(p,Points[i-1],Points[i])<Math.Max(12,Width*3))return true;}return false;
  }
  static double Distance(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
  static double SegmentDistance(PointF p,PointF a,PointF b){float dx=b.X-a.X,dy=b.Y-a.Y;float t=dx*dx+dy*dy==0?0:((p.X-a.X)*dx+(p.Y-a.Y)*dy)/(dx*dx+dy*dy);t=Math.Max(0,Math.Min(1,t));return Distance(p,new PointF(a.X+t*dx,a.Y+t*dy));}
 }
 static class Style {
  public static Color Ink=Color.FromArgb(32,37,47),Blue=Color.FromArgb(0,113,227),Muted=Color.FromArgb(112,120,132);
  public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
  public static void FillRound(Graphics g,RectangleF r,Color color,float radius){using(var p=Round(r,radius))using(var b=new SolidBrush(color))g.FillPath(b,p);}
 }
 partial class Surface:Form {
  public Bitmap Background; public List<Mark> Marks=new List<Mark>();
  List<List<Mark>> undo=new List<List<Mark>>(),redo=new List<List<Mark>>();
  public Tool Selected=Tool.Pen; public Color SelectedColor=Style.Blue; public float Stroke=3;
  Mark current; bool erasing; PointF rawEnd; public int Zoom=200; Point lensPoint=new Point(300,300); public Palette Palette; Palette owner;
  public Surface(Palette o,Bitmap bitmap,Rectangle screen){owner=o;Background=bitmap;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=screen;TopMost=true;DoubleBuffered=true;KeyPreview=true;Cursor=Cursors.Cross;ShowInTaskbar=false;
   InitializeAdvanced();MouseDown+=Down;MouseMove+=PointerMove;MouseUp+=Up;KeyDown+=KeysDown;KeyUp+=(s,e)=>{if(e.KeyCode==Keys.ShiftKey)UpdateShape(false);};MouseWheel+=(s,e)=>{if(Selected==Tool.Magnifier){Zoom=ClampZoom(Zoom+(e.Delta>0?25:-25));Invalidate();}};
   FormClosing+=(s,e)=>{if(Palette!=null&&!Palette.IsDisposed)Palette.Owner=null;}; FormClosed+=(s,e)=>{DisposePages();owner.EndCapture();};
   Palette=o; AutoScaleMode=AutoScaleMode.None; Shown+=(s,e)=>{Palette.Show(this);Palette.BringToFront();Activate();};
  }
  public void Checkpoint(){undo.Add(new List<Mark>(Marks));if(undo.Count>100)undo.RemoveAt(0);redo.Clear();}
  public void Undo(){if(undo.Count==0)return;redo.Add(new List<Mark>(Marks));Marks=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);Invalidate();}
  public void Redo(){if(redo.Count==0)return;undo.Add(new List<Mark>(Marks));Marks=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);Invalidate();}
  public void ClearMarks(){if(Marks.Count==0)return;Checkpoint();Marks.Clear();Invalidate();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.DrawImageUnscaled(Background,0,0);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;if(ShowInk)foreach(var m in Marks)m.Draw(e.Graphics);if(current!=null)current.Draw(e.Graphics);DrawOverlays(e.Graphics);if(Selected==Tool.Magnifier)DrawMagnifier(e.Graphics,lensPoint);}
  void Down(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;if(AdvancedDown(e.Location))return;
   if(Selected==Tool.Magnifier){lensPoint=e.Location;Zoom=Zoom>=400?100:ClampZoom(Zoom+50);Invalidate();return;}
   if(Selected==Tool.Text){using(var input=new TextDialog()){if(input.ShowDialog(Palette)==DialogResult.OK&&!String.IsNullOrWhiteSpace(input.Value)){Checkpoint();var m=new Mark{Tool=Tool.Text,Color=SelectedColor,Width=Stroke,Text=input.Value};ApplyOptions(m);m.Points.Add(e.Location);Marks.Add(m);Invalidate();}}return;}
   if(Selected==Tool.Eraser){Checkpoint();erasing=true;Erase(e.Location);Capture=true;return;}
   current=new Mark{Tool=Selected,Color=SelectedColor,Width=Stroke};ApplyOptions(current);current.Points.Add(e.Location);current.Points.Add(e.Location);rawEnd=e.Location;Capture=true;
  }
  void PointerMove(object s,MouseEventArgs e){lensPoint=e.Location;if(AdvancedMove(e.Location))return;if(Selected==Tool.Magnifier){Invalidate();return;}if(erasing){Erase(e.Location);return;}if(current==null)return;if(current.Tool==Tool.Pen||current.Tool==Tool.Marker)current.Points.Add(e.Location);else {rawEnd=e.Location;UpdateShape((ModifierKeys&Keys.Shift)!=0);}Invalidate();}
  void Up(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;if(AdvancedUp(e.Location))return;erasing=false;if(current!=null){if(current.Tool!=Tool.Pen&&current.Tool!=Tool.Marker){rawEnd=e.Location;UpdateShape((ModifierKeys&Keys.Shift)!=0);}Checkpoint();Marks.Add(current);current=null;Invalidate();}Capture=false;}
  public static int ClampZoom(int value){return Math.Max(100,Math.Min(400,value));}
  public static PointF Constrain(PointF a,PointF b,Tool tool,bool shift){if(!shift)return b;float dx=b.X-a.X,dy=b.Y-a.Y;if(tool==Tool.Rectangle||tool==Tool.Ellipse||tool==Tool.Shape){float side=Math.Max(Math.Abs(dx),Math.Abs(dy));return new PointF(a.X+(dx<0?-side:side),a.Y+(dy<0?-side:side));}if(tool==Tool.Arrow||tool==Tool.Line||tool==Tool.Ruler){double len=Math.Sqrt(dx*dx+dy*dy),angle=Math.Round(Math.Atan2(dy,dx)/(Math.PI/4))*(Math.PI/4);return new PointF(a.X+(float)(len*Math.Cos(angle)),a.Y+(float)(len*Math.Sin(angle)));}return b;}
  void UpdateShape(bool shift){if(current==null||current.Tool==Tool.Pen||current.Tool==Tool.Marker)return;current.Points[current.Points.Count-1]=Constrain(current.Points[0],rawEnd,current.Tool,shift);Invalidate();}
  public void DrawMagnifier(Graphics g,Point point){int w=Math.Min(Options.LensWidth,ClientSize.Width-16),h=Math.Min(Options.LensHeight,ClientSize.Height-16);if(w<40||h<40)return;int x=Math.Max(8,Math.Min(ClientSize.Width-w-8,point.X+28)),y=Math.Max(8,Math.Min(ClientSize.Height-h-8,point.Y+28));var r=new RectangleF(x,y,w,h);Style.FillRound(g,new RectangleF(x+3,y+5,w,h),Color.FromArgb(85,0,0,0),18);var state=g.Save();using(var path=Style.Round(r,18))g.SetClip(path);g.FillRectangle(Brushes.White,r);g.TranslateTransform(x+w/2f,y+(h-30)/2f);float scale=Zoom/100f;g.ScaleTransform(scale,scale);g.TranslateTransform(-point.X,-point.Y);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.DrawImageUnscaled(Background,0,0);if(ShowInk)foreach(var mark in Marks)mark.Draw(g);g.Restore(state);using(var path=Style.Round(r,18))using(var pen=new Pen(Style.Blue,2))g.DrawPath(pen,path);Style.FillRound(g,new RectangleF(x+8,y+h-30,w-16,24),Color.FromArgb(249,250,252),8);using(var f=new Font("Segoe UI",9))using(var b=new SolidBrush(Style.Ink))g.DrawString("Lupa  "+Zoom+"%   ·   role para ajustar",f,b,x+20,y+h-26);}
  void Erase(PointF p){Marks.RemoveAll(m=>m.Hit(p));Invalidate();}
  public void KeysDown(object s,KeyEventArgs e){
   if(AdvancedKeys(e))return;if(e.KeyCode==Keys.ShiftKey)UpdateShape(true);
   if(e.KeyCode==Keys.Escape){Close();e.Handled=true;return;}

  }
  public Bitmap Render(){var b=new Bitmap(Background.Width,Background.Height);using(var g=Graphics.FromImage(b)){g.DrawImageUnscaled(Background,0,0);g.SmoothingMode=SmoothingMode.AntiAlias;foreach(var m in Marks)m.Draw(g);}return b;}
  public void Save(){Print();}
  public void Print(){using(var options=new PrintOptions()){var choice=options.ShowDialog(Palette);if(choice!=DialogResult.OK)return;using(var image=Render()){if(options.Free){Palette.Hide();try{using(var selection=new FreeSelection(image,Bounds)){if(selection.ShowDialog(this)==DialogResult.OK)using(var cropped=selection.RenderSelection())SaveImage(cropped);}}finally{Palette.Show(this);}}else if(options.Area){Palette.Hide();try{using(var selection=new AreaSelection(image,Bounds)){if(selection.ShowDialog(this)==DialogResult.OK){using(var cropped=Crop(image,selection.SelectedArea))SaveImage(cropped);}}}finally{if(!Palette.IsDisposed)Palette.Show(this);}}else SaveImage(image);}}}
  public static Bitmap Crop(Bitmap image,Rectangle area){area=Rectangle.Intersect(new Rectangle(Point.Empty,image.Size),area);if(area.Width<1||area.Height<1)throw new ArgumentException("Selecione uma área da tela.");return image.Clone(area,PixelFormat.Format32bppArgb);}
  void SaveImage(Bitmap image){using(var dialog=new SaveFileDialog()){dialog.Title="Salvar screenshot";dialog.Filter="Imagem PNG|*.png";dialog.FileName="Screenshot-"+DateTime.Now.ToString("yyyy-MM-dd-HHmmss")+".png";if(dialog.ShowDialog(this)==DialogResult.OK){try{image.Save(dialog.FileName,ImageFormat.Png);Palette.Notify("Imagem salva");}catch(Exception ex){MessageBox.Show(this,ex.Message,"Não foi possível salvar");}}}}
  public void Copy(){try{using(var b=Render())Clipboard.SetImage(b);Palette.Notify("Copiado para a área de transferência");}catch(Exception ex){MessageBox.Show(Palette,ex.Message,"Não foi possível copiar");}}
 }
 class PrintOptions:Form {
  public bool Area;public bool Free;
  public PrintOptions(){Text="Print · screenshot";AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(420,260);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;BackColor=Color.FromArgb(249,250,252);Font=new Font("Segoe UI",10);var title=new Label{Text="O que deseja capturar?",Font=new Font("Segoe UI",15,FontStyle.Bold),AutoSize=true,Location=new Point(22,20)};var info=new Label{Text="Escolha uma opção e depois onde salvar a imagem.",AutoSize=true,Location=new Point(22,60)};Controls.Add(title);Controls.Add(info);var full=new Button{Text="Tela inteira",Bounds=new Rectangle(22,99,181,44),DialogResult=DialogResult.OK,FlatStyle=FlatStyle.Flat,BackColor=Style.Blue,ForeColor=Color.White};var area=new Button{Text="Selecionar área",Bounds=new Rectangle(215,99,181,44),FlatStyle=FlatStyle.Flat};area.Click+=(o,e)=>{Area=true;DialogResult=DialogResult.OK;};var free=new Button{Text="Seleção livre",Bounds=new Rectangle(22,156,181,44),FlatStyle=FlatStyle.Flat};free.Click+=(o,e)=>{Free=true;DialogResult=DialogResult.OK;};Controls.Add(free);var cancel=new Button{Text="Cancelar",Bounds=new Rectangle(296,212,100,30),DialogResult=DialogResult.Cancel};Controls.AddRange(new Control[]{full,area,cancel});AcceptButton=full;CancelButton=cancel;}
 }
 class AreaSelection:Form {
  Bitmap image;Point anchor;bool dragging;public Rectangle SelectedArea;
  public AreaSelection(Bitmap source,Rectangle screen){image=source;AutoScaleMode=AutoScaleMode.None;Text="Selecionar área";StartPosition=FormStartPosition.Manual;FormBorderStyle=FormBorderStyle.None;Bounds=screen;TopMost=true;DoubleBuffered=true;Cursor=Cursors.Cross;KeyPreview=true;MouseDown+=(o,e)=>{if(e.Button!=MouseButtons.Left)return;anchor=e.Location;SelectedArea=Rectangle.Empty;dragging=true;Capture=true;};MouseMove+=(o,e)=>{if(dragging){SelectedArea=SelectionBounds(anchor,e.Location,ClientSize);Invalidate();}};MouseUp+=(o,e)=>{if(!dragging||e.Button!=MouseButtons.Left)return;SelectedArea=SelectionBounds(anchor,e.Location,ClientSize);dragging=false;Capture=false;if(SelectedArea.Width>0&&SelectedArea.Height>0)DialogResult=DialogResult.OK;};KeyDown+=(o,e)=>{if(e.KeyCode==Keys.Escape)DialogResult=DialogResult.Cancel;};}
  public static Rectangle SelectionBounds(Point a,Point b,Size size){var area=Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));return Rectangle.Intersect(new Rectangle(Point.Empty,size),area);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.DrawImageUnscaled(image,0,0);using(var shade=new SolidBrush(Color.FromArgb(120,0,0,0)))g.FillRectangle(shade,ClientRectangle);if(SelectedArea.Width>0&&SelectedArea.Height>0){g.DrawImage(image,SelectedArea,SelectedArea,GraphicsUnit.Pixel);using(var p=new Pen(Style.Blue,2))g.DrawRectangle(p,SelectedArea);using(var f=new Font("Segoe UI",11,FontStyle.Bold))g.DrawString(SelectedArea.Width+" × "+SelectedArea.Height,f,Brushes.White,SelectedArea.X+8,SelectedArea.Y+8);}Style.FillRound(g,new RectangleF(20,20,440,42),Color.FromArgb(249,250,252),12);using(var f=new Font("Segoe UI",11))using(var b=new SolidBrush(Style.Ink))g.DrawString("Arraste para selecionar · Esc para cancelar",f,b,34,31);}
 }
 class TextDialog:Form {
  TextBox input=new TextBox();public string Value{get{return input.Text;}}
  public TextDialog(){Text="Adicionar texto";Font=new Font("Segoe UI",11);BackColor=Color.White;ClientSize=new Size(420,160);FormBorderStyle=FormBorderStyle.FixedDialog;StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;MinimizeBox=false;
   var label=new Label{Text="O que você quer escrever?",Location=new Point(22,20),AutoSize=true};input.SetBounds(22,54,376,30);var ok=new Button{Text="Adicionar",DialogResult=DialogResult.OK,BackColor=Style.Blue,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};ok.SetBounds(278,108,120,32);Controls.AddRange(new Control[]{label,input,ok});AcceptButton=ok;var cancel=new Button{DialogResult=DialogResult.Cancel};CancelButton=cancel;
  }
 }
 partial class Palette:Form {
  Surface surface; ToolTip tips=new ToolTip();string notice="ESC para concluir";Timer timer=new Timer();
  string[] names={"Caneta (P)","Marca-texto (M)","Seta (A)","Retângulo (R)","Elipse (O)","Texto (T)","Borracha de traços (E)","Desfazer (Ctrl+Z)","Refazer (Ctrl+Y)","Limpar","Copiar (Ctrl+C)","Print screen · salvar captura","Concluir (Esc)","Lupa (L) · zoom até 400%"};
  Color[] colors={Style.Blue,Color.FromArgb(255,69,58),Color.FromArgb(255,159,10),Color.FromArgb(255,214,10),Color.FromArgb(48,178,104),Color.FromArgb(175,82,222),Style.Ink,Color.White};
  bool pendingPrint;bool compact;bool adjustingWidth; bool capturing; Color chosenColor=Style.Blue; Tool chosenTool=Tool.Pen; float chosenWidth=3;
  public Palette(Surface s){surface=s;Text="ScreenInk";Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ClientSize=new Size(710,194);BackColor=Color.FromArgb(249,250,252);TopMost=true;ShowInTaskbar=true;DoubleBuffered=true;KeyPreview=true;Font=new Font("Segoe UI",9);var wa=Screen.PrimaryScreen.WorkingArea;Location=new Point(wa.Left+(wa.Width-Width)/2,wa.Top+28);
   InitializeToolbar();MouseDown+=(o,e)=>ToolbarDown(e);MouseMove+=(o,e)=>ToolbarMove(e);MouseUp+=(o,e)=>{if(adjustingWidth){SetWidth(e.X);adjustingWidth=false;Capture=false;}};MouseWheel+=(o,e)=>{var pt=PointToClient(MousePosition);if(pt.Y>=SliderY-20&&pt.X>=SliderLeft){chosenWidth=Math.Max(1,Math.Min(24,chosenWidth+(e.Delta>0?1:-1)));SyncOptions();Invalidate();}};
   KeyDown+=(o,e)=>{if(surface!=null)surface.KeysDown(surface,e);else if(e.KeyCode==Keys.Escape)Close();else if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.P)Action(0);};
   FormClosing+=(o,e)=>{if(surface!=null&&!surface.IsDisposed){var active=surface;surface=null;active.Close();}};
   timer.Interval=2500;timer.Tick+=(o,e)=>{notice="ESC para concluir";timer.Stop();Invalidate();};
  }
  void HeaderAction(int x){if(x>=Width-46){Close();return;}if(x>=Width-76){compact=!compact;UpdateToolbarSize();Invalidate();return;}if(surface!=null)surface.Close();WindowState=FormWindowState.Minimized;}
  public static float WidthAt(int x){return (float)Math.Round(Math.Max(1,Math.Min(24,1+(x-340)*23f/304)));}
  void SetWidth(int x){chosenWidth=(float)Math.Round(Math.Max(1,Math.Min(24,1+(x-SliderLeft)*23f/(SliderRight-SliderLeft))));SyncOptions();Invalidate();}
  void CaptureScreen(){if(capturing||surface!=null)return;capturing=true;Hide();var delay=new Timer{Interval=200};delay.Tick+=(o,e)=>{delay.Stop();delay.Dispose();Bitmap bitmap=null;try{var screen=SystemInformation.VirtualScreen;bitmap=new Bitmap(screen.Width,screen.Height);using(var g=Graphics.FromImage(bitmap))g.CopyFromScreen(screen.Location,Point.Empty,screen.Size);surface=new Surface(this,bitmap,screen){Selected=chosenTool,SelectedColor=chosenColor,Stroke=chosenWidth};bitmap=null;SyncOptions();surface.Show();if(pendingAdvanced>=0){int next=pendingAdvanced;pendingAdvanced=-1;BeginInvoke((Action)(()=>AdvancedAction(next)));}if(pendingPrint){pendingPrint=false;BeginInvoke((Action)(()=>{if(surface!=null)surface.Print();}));}}catch(Exception ex){if(bitmap!=null)bitmap.Dispose();Show();MessageBox.Show(this,ex.Message,"Não foi possível capturar");}finally{capturing=false;}};delay.Start();}
  public void EndCapture(){surface=null;desktopMode=false;if(!IsDisposed&&!Disposing){Show();Activate();Invalidate();}}
  [DllImport("user32.dll")]static extern bool ReleaseCapture();[DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
  protected override void Dispose(bool disposing){if(disposing){timer.Dispose();tips.Dispose();if(advancedMenu!=null)advancedMenu.Dispose();}base.Dispose(disposing);}
  public void Notify(string text){notice=text;timer.Start();Invalidate();}
  void Action(int n){if(n>=20){AdvancedAction(n);return;}if(n==11&&surface==null){pendingPrint=true;CaptureScreen();return;}if(n<7||n==13){chosenTool=n==13?Tool.Magnifier:(Tool)n;if(surface==null)CaptureScreen();else{surface.Selected=chosenTool;surface.Invalidate();surface.Activate();}}else if(n==12){if(surface!=null)surface.Close();else Close();return;}else if(surface!=null){if(n==7)surface.Undo();else if(n==8)surface.Redo();else if(n==9)surface.ClearMarks();else if(n==10)surface.Copy();else if(n==11)surface.Save();if(surface!=null)surface.Activate();}Invalidate();}
  [StructLayout(LayoutKind.Sequential)]struct NativePoint{public int X,Y;public NativePoint(int x,int y){X=x;Y=y;}}
  [StructLayout(LayoutKind.Sequential)]struct NativeSize{public int Width,Height;public NativeSize(int w,int h){Width=w;Height=h;}}
  [StructLayout(LayoutKind.Sequential,Pack=1)]struct Blend{public byte Op,Flags,Alpha,Format;}
  [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr h);
  [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr h,IntPtr dc);
  [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
  [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
  [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr screen,ref NativePoint position,ref NativeSize size,IntPtr memory,ref NativePoint origin,int key,ref Blend blend,int flags);
  protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ExStyle|=0x80000;return cp;}}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);BeginInvoke((Action)RefreshVisual);}
  protected override void OnInvalidated(InvalidateEventArgs e){base.OnInvalidated(e);if(IsHandleCreated&&!IsDisposed)RefreshVisual();}
  void RefreshVisual(){if(IsDisposed||Disposing||!IsHandleCreated||visible==null||Prefs==null)return;using(var bitmap=new Bitmap(Width,Height,PixelFormat.Format32bppArgb)){
   using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;Style.FillRound(g,new RectangleF(2,8,Width-2,Height-8),Color.FromArgb(100,155,165,181),23);DrawToolbar(g);}
   IntPtr screen=GetDC(IntPtr.Zero),memory=CreateCompatibleDC(screen),image=bitmap.GetHbitmap(Color.FromArgb(0)),old=SelectObject(memory,image);
   try{var position=new NativePoint(Left,Top);var size=new NativeSize(Width,Height);var origin=new NativePoint(0,0);var blend=new Blend{Op=0,Alpha=(byte)Prefs.ToolbarOpacity,Format=1};if(!UpdateLayeredWindow(Handle,screen,ref position,ref size,memory,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception();}finally{SelectObject(memory,old);DeleteObject(image);DeleteDC(memory);ReleaseDC(IntPtr.Zero,screen);}
  }}
  public static void PaintPalette(Graphics g,Tool selected,Color color,string status,float width=3,bool compact=false){
   g.SmoothingMode=SmoothingMode.AntiAlias;Style.FillRound(g,new RectangleF(0,0,700,compact?38:178),Color.FromArgb(249,250,252),20);
   using(var pen=new Pen(Color.FromArgb(220,225,232)))using(var path=Style.Round(new RectangleF(.5f,.5f,699,compact?37:177),20))g.DrawPath(pen,path);
   using(var f=new Font("Segoe UI",10,FontStyle.Bold))using(var b=new SolidBrush(Style.Ink))g.DrawString("ScreenInk",f,b,20,14);
   using(var f=new Font("Segoe UI",8))using(var b=new SolidBrush(Style.Muted)){g.DrawString("MODERN",f,b,101,17);g.DrawString(status,f,b,430,17);g.DrawString("Espessura",f,b,270,134);}
   using(var p=new Pen(Style.Ink,1.6f)){g.DrawLine(p,613,23,625,23);g.DrawRectangle(p,644,15,10,10);g.DrawLines(p,new PointF[]{new PointF(647,12),new PointF(657,12),new PointF(657,22)});g.DrawLine(p,675,15,686,26);g.DrawLine(p,686,15,675,26);}if(compact)return;
   for(int i=0;i<13;i++){int toolIndex=i==12?13:i;var r=new RectangleF(14+i*48,43,44,42);if(i==(selected==Tool.Magnifier?12:(int)selected))Style.FillRound(g,r,Style.Blue,11);DrawIcon(g,toolIndex,new RectangleF(r.X+12,r.Y+11,20,20),i==(selected==Tool.Magnifier?12:(int)selected)?Color.White:Style.Ink);}
   string[] labels={"Pincel","Marca-texto","Seta","Retângulo","Elipse","Texto","Borracha","Desfazer","Refazer","Limpar","Copiar","Print","Lupa"};
   using(var f=new Font("Segoe UI",7,FontStyle.Regular,GraphicsUnit.Point))using(var brush=new SolidBrush(Style.Muted))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})for(int i=0;i<labels.Length;i++)g.DrawString(labels[i],f,brush,new RectangleF(12+i*48,88,48,22),format);
   using(var track=new Pen(Color.FromArgb(219,225,235),4)){track.StartCap=track.EndCap=LineCap.Round;g.DrawLine(track,340,141,644,141);}float thumb=340+(width-1)*304/23, radius=4+width*.4f;
   using(var track=new Pen(Style.Blue,4)){track.StartCap=track.EndCap=LineCap.Round;g.DrawLine(track,340,141,thumb,141);}using(var brush=new SolidBrush(color))g.FillEllipse(brush,thumb-radius,141-radius,radius*2,radius*2);using(var pen=new Pen(Color.White,2))g.DrawEllipse(pen,thumb-radius,141-radius,radius*2,radius*2);
   using(var f=new Font("Segoe UI",7))using(var brush=new SolidBrush(Style.Muted))g.DrawString(width+" px",f,brush,658,134);
   Color[] cs={Style.Blue,Color.FromArgb(255,69,58),Color.FromArgb(255,159,10),Color.FromArgb(255,214,10),Color.FromArgb(48,178,104),Color.FromArgb(175,82,222),Style.Ink,Color.White};
   for(int i=0;i<cs.Length;i++){if(cs[i].ToArgb()==color.ToArgb())using(var p=new Pen(Style.Blue,2))g.DrawEllipse(p,18+i*30,128,26,26);using(var b=new SolidBrush(cs[i]))g.FillEllipse(b,22+i*30,132,18,18);if(i==7)using(var p=new Pen(Color.LightGray))g.DrawEllipse(p,22+i*30,132,18,18);}
   using(var p=new Pen(Color.FromArgb(225,229,236)))g.DrawLine(p,18,114,682,114);
  }
  static void DrawIcon(Graphics g,int n,RectangleF r,Color c){if(ReferenceIcons.Draw(g,n,r,c))return;float x=r.X,y=r.Y;using(var p=new Pen(c,1.8f)){p.StartCap=p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;
   if(n==0){g.DrawLines(p,new PointF[]{new PointF(x+8,y+13),new PointF(x+15,y+2),new PointF(x+18,y+4),new PointF(x+12,y+16)});using(var path=new GraphicsPath()){path.AddBezier(x+8,y+13,x+2,y+13,x+7,y+20,x+1,y+21);path.AddBezier(x+1,y+21,x+10,y+23,x+14,y+19,x+12,y+16);path.CloseFigure();using(var b=new SolidBrush(c))g.FillPath(b,path);}}
   if(n==1){g.DrawPolygon(p,new PointF[]{new PointF(x+4,y+12),new PointF(x+13,y+2),new PointF(x+19,y+7),new PointF(x+10,y+17)});g.DrawLines(p,new PointF[]{new PointF(x+4,y+12),new PointF(x+2,y+17),new PointF(x+7,y+19),new PointF(x+10,y+17)});p.Width=3;g.DrawLine(p,x+1,y+22,x+18,y+22);}
   if(n==13){g.DrawEllipse(p,x+1,y+1,13,13);p.Width=2.4f;g.DrawLine(p,x+13,y+13,x+20,y+20);}
   if(n==2){g.DrawLine(p,x+2,y+18,x+18,y+2);g.DrawLines(p,new PointF[]{new PointF(x+7,y+2),new PointF(x+18,y+2),new PointF(x+18,y+13)});}
   if(n==3)g.DrawRectangle(p,x+1,y+3,18,14);if(n==4)g.DrawEllipse(p,x+1,y+2,18,16);
   if(n==5){g.DrawLine(p,x+2,y+3,x+18,y+3);g.DrawLine(p,x+10,y+3,x+10,y+19);g.DrawLine(p,x+6,y+19,x+14,y+19);}
   if(n==6)g.DrawPolygon(p,new PointF[]{new PointF(x+2,y+12),new PointF(x+12,y+2),new PointF(x+20,y+10),new PointF(x+10,y+20),new PointF(x+2,y+12)});
   if(n==7||n==8){g.DrawArc(p,x+3,y+5,14,13,n==7?160:10,240);if(n==7)g.DrawLines(p,new PointF[]{new PointF(x+2,y+3),new PointF(x+2,y+10),new PointF(x+9,y+10)});else g.DrawLines(p,new PointF[]{new PointF(x+18,y+3),new PointF(x+18,y+10),new PointF(x+11,y+10)});}
   if(n==9){g.DrawRectangle(p,x+5,y+6,11,13);g.DrawLine(p,x+3,y+4,x+18,y+4);g.DrawLine(p,x+8,y+1,x+13,y+1);}
   if(n==10){g.DrawRectangle(p,x+7,y+6,12,13);g.DrawLines(p,new PointF[]{new PointF(x+4,y+15),new PointF(x+1,y+15),new PointF(x+1,y+1),new PointF(x+13,y+1),new PointF(x+13,y+3)});}
   if(n==11){g.DrawLine(p,x+10,y+1,x+10,y+13);g.DrawLines(p,new PointF[]{new PointF(x+5,y+9),new PointF(x+10,y+14),new PointF(x+15,y+9)});g.DrawLines(p,new PointF[]{new PointF(x+2,y+14),new PointF(x+2,y+20),new PointF(x+18,y+20),new PointF(x+18,y+14)});}
   if(n==12)g.DrawLines(p,new PointF[]{new PointF(x+2,y+10),new PointF(x+8,y+16),new PointF(x+19,y+4)});
  }}
 }
 static class ReferenceIcons {
  static Dictionary<int,Bitmap> icons=new Dictionary<int,Bitmap>();
  static ReferenceIcons(){Load(1,"marker",0);Load(6,"eraser",0);Load(7,"history",-1);Load(8,"history",1);Load(9,"trash",0);Load(11,"screenshot",0);}
  static void Load(int key,string name,int half){using(var stream=typeof(Program).Assembly.GetManifestResourceStream(name+".png"))using(var source=new Bitmap(stream)){
   int left=half==1?source.Width/2:0,right=half==-1?source.Width/2:source.Width;var image=new Bitmap(right-left,source.Height,PixelFormat.Format32bppArgb);int minX=image.Width,minY=image.Height,maxX=-1,maxY=-1;
   for(int y=0;y<source.Height;y++)for(int x=left;x<right;x++){var c=source.GetPixel(x,y);int alpha=(255-Math.Max(c.R,Math.Max(c.G,c.B)))*c.A/255;image.SetPixel(x-left,y,Color.FromArgb(alpha,255,255,255));if(alpha>20){minX=Math.Min(minX,x-left);maxX=Math.Max(maxX,x-left);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}}
   if(maxX<0)throw new Exception("Empty icon: "+name);icons[key]=image.Clone(Rectangle.FromLTRB(minX,minY,maxX+1,maxY+1),PixelFormat.Format32bppArgb);image.Dispose();
  }}
  public static bool Draw(Graphics g,int n,RectangleF rect,Color color){Bitmap image;if(!icons.TryGetValue(n,out image))return false;float scale=Math.Min(rect.Width/image.Width,rect.Height/image.Height);var dest=new Rectangle((int)(rect.X+(rect.Width-image.Width*scale)/2),(int)(rect.Y+(rect.Height-image.Height*scale)/2),(int)(image.Width*scale),(int)(image.Height*scale));using(var attributes=new ImageAttributes()){var matrix=new ColorMatrix(new float[][]{new float[]{color.R/255f,0,0,0,0},new float[]{0,color.G/255f,0,0,0},new float[]{0,0,color.B/255f,0,0},new float[]{0,0,0,1,0},new float[]{0,0,0,0,1}});attributes.SetColorMatrix(matrix);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,dest,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);}return true;}
 }
 static class Program {
  [DllImport("user32.dll")]static extern bool SetProcessDpiAwarenessContext(IntPtr context);
  [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
  [STAThread]static void Main(string[] args){try{SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{SetProcessDPIAware();}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   if(args.Length>0&&args[0]=="--toolbar-preview"){using(var b=new Bitmap(776,225))using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(233,237,245));g.SmoothingMode=SmoothingMode.AntiAlias;Style.FillRound(g,new RectangleF(20,36,708,186),Color.FromArgb(207,213,223),23);g.TranslateTransform(18,28);Palette.PaintPalette(g,Tool.Pen,Style.Blue,"ESC para concluir");b.Save(args[1],ImageFormat.Png);}return;}
   if(args.Length>0&&args[0]=="--preview"){Preview(args.Length>1?args[1]:"preview.png");return;}
   if(args.Length>0&&args[0]=="--self-test"){Application.ThreadException+=(sender,eventArgs)=>{File.WriteAllText(args[1]+".thread-failure.txt",eventArgs.Exception.ToString());Environment.Exit(1);};try{SelfTest(args[1]);AdvancedTests.Run(Path.ChangeExtension(args[1],"advanced"));}catch(Exception failure){File.WriteAllText(args[1]+".failure.txt",failure.ToString());Environment.ExitCode=1;}return;}
   try { Application.Run(new Palette(null)); } catch(Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-error.txt"),ex.ToString()); }
  }
  static void SelfTest(string output){using(var image=new Bitmap(100,80)){image.SetPixel(20,30,Color.Red);using(var crop=Surface.Crop(image,new Rectangle(20,30,40,20))){if(crop.Width!=40||crop.Height!=20||crop.GetPixel(0,0).ToArgb()!=Color.Red.ToArgb())throw new Exception("Crop pixels and size");}var r=AreaSelection.SelectionBounds(new Point(90,70),new Point(-10,-10),image.Size);if(r!=new Rectangle(0,0,90,70))throw new Exception("Reverse and clipped selection");}foreach(var tool in new[]{Tool.Rectangle,Tool.Ellipse})foreach(var b in new[]{new PointF(200,140),new PointF(-100,140),new PointF(200,-40),new PointF(-100,-40)}){var a=new PointF(50,50);var end=Surface.Constrain(a,b,tool,true);if(Math.Abs(Math.Abs(end.X-a.X)-Math.Abs(end.Y-a.Y))>.01)throw new Exception("Shift proportion");if(Surface.Constrain(a,b,tool,false)!=b)throw new Exception("Free proportion");}if(Surface.ClampZoom(450)!=400||Surface.ClampZoom(50)!=100||Surface.ClampZoom(250)!=250)throw new Exception("Zoom bounds");if(Palette.WidthAt(340)!=1||Palette.WidthAt(644)!=24||Palette.WidthAt(500)<=Palette.WidthAt(400))throw new Exception("Slider mapping");using(var source=new Bitmap(640,480)){using(var g=Graphics.FromImage(source))g.Clear(Color.White);using(var launcher=new Palette(null))using(var surface=new Surface(launcher,(Bitmap)source.Clone(),new Rectangle(0,0,640,480))){var mark=new Mark{Tool=Tool.Rectangle,Color=Color.Red,Width=4};mark.Points.Add(new PointF(100,100));mark.Points.Add(new PointF(250,200));surface.Checkpoint();surface.Marks.Add(mark);surface.Undo();if(surface.Marks.Count!=0)throw new Exception("Undo failed");surface.Redo();if(surface.Marks.Count!=1)throw new Exception("Redo failed");using(var image=surface.Render()){if(image.GetPixel(100,150).G>100)throw new Exception("Render failed");image.Save(output,ImageFormat.Png);}surface.ClearMarks();surface.Undo();if(surface.Marks.Count!=1)throw new Exception("Clear undo failed");if(!mark.Hit(new PointF(150,100)))throw new Exception("Hit test failed");
 surface.Marks.Clear();surface.Background.SetPixel(53,50,Color.Red);surface.Zoom=400;using(var lens=new Bitmap(640,480))using(var lg=Graphics.FromImage(lens)){surface.DrawMagnifier(lg,new Point(50,50));bool red=false;for(int x=248;x<=254;x++)for(int y=180;y<=186;y++){var c=lens.GetPixel(x,y);if(c.R>200&&c.G<50)red=true;}if(!red)throw new Exception("Magnifier 400 percent image sampling failed");}
 var diagonal=Surface.Constrain(new PointF(0,0),new PointF(70,40),Tool.Arrow,true);if(Math.Abs(diagonal.X-diagonal.Y)>.01)throw new Exception("Shift arrow angle");

} }File.WriteAllText(output+".txt","PASS: render, undo, redo, clear undo, hit test, Shift proportions in four quadrants, zoom 100-400%, slider limits and direction, real 400% lens image sampling, Shift arrow alignment, crop pixel identity, reversed/clipped area selection");}
  static void Preview(string output){using(var b=new Bitmap(1440,900))using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(233,237,245));Style.FillRound(g,new RectangleF(170,235,1100,555),Color.White,24);
   using(var f=new Font("Segoe UI",11))using(var brush=new SolidBrush(Style.Muted))g.DrawString("DOCUMENTO DE EXEMPLO · PRÉVIA DA INTERFACE",f,brush,225,278);
   using(var f=new Font("Segoe UI",34,FontStyle.Bold))using(var brush=new SolidBrush(Style.Ink))g.DrawString("Ideias que merecem destaque.",f,brush,225,333);
   using(var f=new Font("Segoe UI",18))using(var brush=new SolidBrush(Style.Muted)){g.DrawString("Uma apresentação mais clara começa com uma boa anotação.",f,brush,228,420);g.DrawString("Marque o essencial. Conecte os pontos. Compartilhe a imagem.",f,brush,228,465);}
   var marker=new Mark{Tool=Tool.Marker,Color=Color.FromArgb(255,214,10),Width=9};marker.Points.Add(new PointF(228,452));marker.Points.Add(new PointF(548,452));marker.Draw(g);
   var arrow=new Mark{Tool=Tool.Arrow,Color=Style.Blue,Width=4};arrow.Points.Add(new PointF(1010,635));arrow.Points.Add(new PointF(940,485));arrow.Draw(g);
   using(var f=new Font("Segoe UI",20))using(var brush=new SolidBrush(Style.Blue))g.DrawString("Foco aqui",f,brush,946,650);
   using(var f=new Font("Segoe UI",12))using(var brush=new SolidBrush(Style.Muted))g.DrawString("POINTOfix MODERN  /  CAPTURAR · ANOTAR · COMPARTILHAR",f,brush,225,726);
   Style.FillRound(g,new RectangleF(396,56,660,154),Color.FromArgb(35,40,50,65),23);g.TranslateTransform(394,48);Palette.PaintPalette(g,Tool.Pen,Style.Blue,"ESC para concluir");g.ResetTransform();b.Save(output,ImageFormat.Png);
  }
 }
}
}









