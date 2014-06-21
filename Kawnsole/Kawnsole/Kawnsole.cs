// Using SDL2-CS https://github.com/flibitijibibo/SDL2-CS and original SDL2 and SDL2_image files
// Using tileset from http://dwarffortresswiki.org/index.php/Tileset_repository

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.IO;

using KeyCode = SDL2.SDL.SDL_Keycode;

using SDL = SDL2.SDL;
using SDLi = SDL2.SDL_image;
using SDLm = SDL2.SDL_mixer;

using SDLClr = SDL2.SDL.SDL_Color;
using CONCLR = SDL2.SDL.SDL_Color;

namespace Kawnsole {
	public static class Kawnsole {
		private static Stopwatch SWatch = new Stopwatch();

		private static IntPtr Wind, Rend, FontTex, PixelFormat;

		private static bool Open = false, Ctrl = false, Shift = false, Alt = false, Initialized = false, DoRefresh = true;
		private static bool FontDirty = false;
		private static KeyCode KC;
		private static Queue<KawnsoleInput> InputQueue;

		private static SDL.SDL_Event Event;
		private static StringBuilder Input;

		private static int W = 80, H = 40, CharW = 8, CharH = 8, FontW = 0, FontH = 0, CharCount, CharCountX, CharCountY;
		private static string FontPath, WindTitle = "[NULL]";
		private static bool ReloadSDL = false;
		private static SDL.SDL_Rect POS, TEXPOS;

		private static char[] TEXT;
		private static CONCLR[] FORE_C, BACK_C;
		private static bool[] DIRTY;

		public static long FrameTime;

		private static void CreateSDL() {
			if (Rend != IntPtr.Zero) {
				SDL.SDL_DestroyRenderer(Rend);
				Rend = IntPtr.Zero;
			}
			if (Wind != IntPtr.Zero) {
				SDL.SDL_DestroyWindow(Wind);
				Wind = IntPtr.Zero;
			}
			if (FontTex != IntPtr.Zero) {
				SDL.SDL_FreeSurface(FontTex);
				FontTex = IntPtr.Zero;
			}

			Wind = SDL.SDL_CreateWindow(WindTitle, 100, 100, 0, 0, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
			Rend = SDL.SDL_CreateRenderer(Wind, -1,
				(uint)(SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC));

			PixelFormat = SDL.SDL_AllocFormat(SDL.SDL_GetWindowPixelFormat(Wind));

			SetFont(FontPath);
		}

		private unsafe static void Main() {
			if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
				throw new Exception(SDL.SDL_GetError());
			if (SDLi.IMG_Init(SDLi.IMG_InitFlags.IMG_INIT_PNG) == 0)
				throw new Exception(SDL.SDL_GetError());

			CreateSDL();

			Open = true;
			Initialized = true;
			bool Changed = false;

			SWatch.Start();

			while (Open) {
				while (SDL.SDL_PollEvent(out Event) != 0) {
					switch (Event.type) {
						case SDL.SDL_EventType.SDL_QUIT:
							Open = false;
							break;
						case SDL.SDL_EventType.SDL_TEXTINPUT:
							fixed (byte* T = Event.text.text) {
								char Chr = (char)T[0];
								InputQueue.Enqueue(new KawnsoleInput(Chr, KC, Ctrl, Shift, Alt));
							}
							break;

						case SDL.SDL_EventType.SDL_KEYDOWN: {
								KeyCode KC = Event.key.keysym.sym;

								if ((KC == KeyCode.SDLK_LCTRL) || (KC == KeyCode.SDLK_RCTRL))
									Ctrl = true;
								if ((KC == KeyCode.SDLK_LALT) || (KC == SDL.SDL_Keycode.SDLK_RALT))
									Alt = true;
								if ((KC == KeyCode.SDLK_LSHIFT) || (KC == KeyCode.SDLK_RSHIFT))
									Shift = true;

								Kawnsole.KC = KC;

								switch (KC) {
									case KeyCode.SDLK_UP:
									case KeyCode.SDLK_DOWN:
									case KeyCode.SDLK_LEFT:
									case KeyCode.SDLK_RIGHT:
									case KeyCode.SDLK_RETURN:
									case KeyCode.SDLK_RETURN2:
									case KeyCode.SDLK_BACKSPACE:
										InputQueue.Enqueue(new KawnsoleInput('\n', KC, Ctrl, Shift, Alt));
										break;
									default:
										break;
								}
								break;
							}

						case SDL.SDL_EventType.SDL_KEYUP: {
								KeyCode KC = Event.key.keysym.sym;
								if ((KC == KeyCode.SDLK_LCTRL) || (KC == KeyCode.SDLK_RCTRL))
									Ctrl = false;
								if ((KC == KeyCode.SDLK_LALT) || (KC == KeyCode.SDLK_RALT))
									Alt = false;
								if ((KC == KeyCode.SDLK_LSHIFT) || (KC == KeyCode.SDLK_RSHIFT))
									Shift = false;
								break;
							}

						default:
							break;

					}
				}

				if (ReloadSDL) {
					ReloadSDL = false;
					CreateSDL();
				}

				SDL.SDL_SetRenderDrawBlendMode(Rend, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
				byte CurChar = 0;

				if (DoRefresh) {
					DoRefresh = Changed = false;

					for (int i = 0; i < TEXT.Length; i++) {
						if (!DIRTY[i])
							continue;
						DIRTY[i] = false;
						Changed = true;

						POS.x = (i % W) * CharW;
						POS.y = (i / W) * CharH;
						CurChar = (byte)TEXT[i];
						TEXPOS.x = (CurChar % CharCountX) * CharW;
						TEXPOS.y = (CurChar / CharCountY) * CharH;

						fixed (void* TP = &TEXPOS, P = &POS) {
							SDL.SDL_SetRenderDrawColor(Rend, BACK_C[i].r, BACK_C[i].g, BACK_C[i].b, BACK_C[i].a);
							SDL.SDL_RenderFillRect(Rend, ref POS);

							SDL.SDL_SetTextureColorMod(FontTex, FORE_C[i].r, FORE_C[i].g, FORE_C[i].b);
							SDL.SDL_SetTextureAlphaMod(FontTex, FORE_C[i].a);
							SDL.SDL_RenderCopy(Rend, FontTex, new IntPtr(TP), new IntPtr(P));
						}
					}

					if (Changed)
						SDL.SDL_RenderPresent(Rend);
				}

				SDL.SDL_Delay(10);

				SWatch.Stop();
				FrameTime = SWatch.ElapsedMilliseconds;
				SWatch.Restart();

				if (FontDirty) {
					FontDirty = false;
					SetFont(Kawnsole.FontPath, CharCountX, CharCountY);
				}
			}

			SDL.SDL_DestroyRenderer(Rend);
			SDL.SDL_DestroyWindow(Wind);
			Environment.Exit(0);
		}

		public static void Set(int i, char Chr, Color Fore, Color Back) {
			if (TEXT[i] != Chr ||
				FORE_C[i].r != Fore.R || FORE_C[i].g != Fore.G || FORE_C[i].b != Fore.B || FORE_C[i].a != Fore.A ||
				BACK_C[i].r != Back.R || BACK_C[i].g != Back.G || BACK_C[i].b != Back.B || BACK_C[i].a != Back.A) {

				TEXT[i] = Chr;

				FORE_C[i].r = Fore.R;
				FORE_C[i].g = Fore.G;
				FORE_C[i].b = Fore.B;
				FORE_C[i].a = Fore.A;

				BACK_C[i].r = Back.R;
				BACK_C[i].g = Back.G;
				BACK_C[i].b = Back.B;
				BACK_C[i].a = Back.A;

				DIRTY[i] = true;
			}
		}

		// TODO, switch buffers here/proper double buffering
		public static void Refresh() {
			DoRefresh = true;
		}

		public static void Initialize(string FontPath) {
			Kawnsole.FontPath = FontPath;

			InputQueue = new Queue<KawnsoleInput>();
			Input = new StringBuilder();

			Thread CartRuntime = new Thread(Main);
			CartRuntime.Start();

			while (!Initialized)
				;

			Refresh();
		}

		public static string Title {
			get {
				return SDL.SDL_GetWindowTitle(Wind);
			}
			set {
				SDL.SDL_SetWindowTitle(Wind, value);
				WindTitle = value;
			}
		}

		public static bool KeyAvailable {
			get {
				return InputQueue.Count > 0;
			}
		}

		public static int Width {
			get {
				int w = 0, h = 0;
				SDL.SDL_GetWindowSize(Wind, out w, out h);
				return w / CharW;
			}
			set {
				int w = 0, h = 0;
				SDL.SDL_GetWindowSize(Wind, out w, out h);
				SetSize(value, h / CharH);
			}
		}

		public static int Height {
			get {
				int w = 0, h = 0;
				SDL.SDL_GetWindowSize(Wind, out w, out h);
				return h / CharH;
			}
			set {
				int w = 0, h = 0;
				SDL.SDL_GetWindowSize(Wind, out w, out h);
				SetSize(w / CharW, value);
			}
		}

		public static int Length {
			get {
				return CharCount;
			}
		}

		public static void Clear() {
			Pos = 0;
			for (int i = 0; i < Length; i++)
				Set(i, ' ', Color.LightGray, Color.Black);
		}

		/// <summary>
		/// Set window size in characters
		/// </summary>
		/// <param name="W">Width in chars</param>
		/// <param name="H">Height in chars</param>
		public static void SetSize(int W, int H) {
			Kawnsole.W = W;
			Kawnsole.H = H;
			SDL.SDL_SetWindowSize(Wind, W * CharW, H * CharH);
			CharCount = W * H;

			char[] OTEXT = TEXT;
			TEXT = new char[CharCount];

			CONCLR[] OFORE_C = FORE_C, OBACK_C = BACK_C;
			FORE_C = new CONCLR[CharCount];
			BACK_C = new CONCLR[CharCount];

			DIRTY = new bool[CharCount];

			if (OTEXT != null && OFORE_C != null && OBACK_C != null) {
				int L = Math.Min(TEXT.Length, OTEXT.Length);
				for (int i = 0; i < L; i++) {
					TEXT[i] = OTEXT[i];
					FORE_C[i] = OFORE_C[i];
					BACK_C[i] = OBACK_C[i];
					DIRTY[i] = true;
				}
			}
		}

		private static FileSystemWatcher FntWatcher;

		public static void FontWatcher(bool Enable = true) {
			if (Enable) {
				FntWatcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(Kawnsole.FontPath)));
				FntWatcher.Changed += (S, E) => {
					if (E.Name == "font.png") {
						if (E.ChangeType != WatcherChangeTypes.Changed)
							throw new Exception("Font file has been moved/renamed/deleted!");
						FontDirty = true;
					}
				};
				FntWatcher.EnableRaisingEvents = true;
			} else if (FntWatcher != null) {
				FntWatcher.EnableRaisingEvents = false;
				FntWatcher.Dispose();
				FntWatcher = null;
			}
		}

		public static void SetFont(string Path, int CharCountX = 16, int CharCountY = 16) {
			Kawnsole.FontPath = Path;
			Kawnsole.CharCountX = CharCountX;
			Kawnsole.CharCountY = CharCountY;

			if (FontTex != IntPtr.Zero) {
				SDL.SDL_FreeSurface(FontTex);
				FontTex = IntPtr.Zero;
			}

			FontTex = SDLi.IMG_Load(Path);
			if (FontTex == IntPtr.Zero)
				throw new Exception("Could not load font:\n" + Path);

			SDL.SDL_SetColorKey(FontTex, 1, SDL.SDL_MapRGB(PixelFormat, 255, 0, 255));
			SDL.SDL_SetSurfaceBlendMode(FontTex, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
			var FontTexTmp = SDL.SDL_CreateTextureFromSurface(Rend, FontTex);
			SDL.SDL_FreeSurface(FontTex);
			FontTex = FontTexTmp;

			uint Format = 0;
			int Access = 0;

			if (SDL.SDL_QueryTexture(FontTex, out Format, out Access, out FontW, out FontH) != 0)
				throw new Exception(SDL.SDL_GetError());


			POS = new SDL.SDL_Rect();
			TEXPOS = new SDL.SDL_Rect();

			POS.x = POS.y = 0;

			TEXPOS.w = POS.w = CharW = FontW / CharCountX;
			TEXPOS.h = POS.h = CharH = FontH / CharCountY;

			SetSize(W, H);
		}

		public static void Write(int I, string S, Color FG, Color BG) {
			Pos = I;
			for (int i = 0; i < S.Length; i++) {
				switch (S[i]) {
					case '\r':
					case '\n':
						Pos += W - Pos % W;
						break;
					case '\t': {
							int Spacing = 8;
							int M = (Pos / W) % Spacing;
							Pos += M == 0 ? Spacing : M;
							break;
						}
					default:
						Set(Pos++, S[i], FG, BG);
						break;
				}
			}

			Refresh();
		}

		public static void Write(int X, int Y, string S, Color FG, Color BG) {
			Write(Y * W + X, S, FG, BG);
		}

		public static Color Foreground = Color.Gray;
		public static Color Background = Color.Black;
		public static int Pos = 0;


		public static void Write(object O, params object[] P) {
			string S = O.ToString();
			if (P != null)
				S = string.Format(O.ToString(), P);

			Write(Pos, S, Foreground, Background);
		}

		public static void WriteLine(object O, params object[] P) {
			string S = O.ToString();
			if (P != null)
				S = string.Format(O.ToString(), P);

			Write("{0}\n", S);
		}

		public static KawnsoleInput ReadKey(bool intercept = false) {
			while (!KeyAvailable)
				;
			return InputQueue.Dequeue();
		}

		public static string ReadLine() {
			Input.Clear();

			KawnsoleInput CCI = ReadKey();
			while ((CCI.Key != KeyCode.SDLK_RETURN) && (CCI.Key != KeyCode.SDLK_RETURN2)) {
				if (CCI.Chr != '\0')
					Input.Append(CCI.Chr);
				if (CCI.Key == KeyCode.SDLK_BACKSPACE) {
					string In = Input.ToString(0, Input.Length - 1);
					Input.Clear();
					Input.Append(In);
				}
				CCI = ReadKey();
			}

			return Input.ToString();
		}

	}
}