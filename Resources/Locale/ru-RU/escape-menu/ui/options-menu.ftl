## General stuff

ui-options-title = Игровые настройки
ui-options-tab-graphics = Графика
ui-options-tab-controls = Управление
ui-options-tab-audio = Аудио
ui-options-tab-network = Сеть

ui-options-apply = Применить
ui-options-reset-all = Отменить
ui-options-default = Сбросить

## Audio menu

ui-options-master-volume = Главный канал:
ui-options-midi-volume = MIDI канал:
ui-options-ambience-volume = Канал фоновых звуков:
ui-options-ambience-music-volume = Канал фоновой музыки:
ui-options-ambience-max-sounds = Одномоментно фоновых звуков:
ui-options-lobby-music = Музыка в лобби и перед концом раунда
ui-options-restart-sounds = Звуки при перезапуске
ui-options-event-music = Музыка событий
ui-options-admin-sounds = Админ-музыка
ui-options-ambience-music = Фоновая музыка
ui-options-volume-label = Громкость
ui-options-volume-percent = { TOSTRING($volume, "P0") }
ui-options-volume-ui = Громкость UI:

## Graphics menu

ui-options-show-held-item = Показывать иконку предмета в руке у курсора?
ui-options-vsync = VSync
ui-options-fullscreen = Полноэкранный режим
ui-options-lighting-label = Качество освщения:
ui-options-lighting-very-low = Очень низкое
ui-options-lighting-low = Низкое
ui-options-lighting-medium = Среднее
ui-options-lighting-high = Высокое
ui-options-scale-label = Масштаб UI:
ui-options-scale-auto = Автоматический ({ TOSTRING($scale, "P0") })
ui-options-scale-75 = 75%
ui-options-scale-100 = 100%
ui-options-scale-125 = 125%
ui-options-scale-150 = 150%
ui-options-scale-175 = 175%
ui-options-scale-200 = 200%
ui-options-hud-theme = Тема HUD:
ui-options-hud-theme-default = Default
ui-options-hud-theme-modernized = Modernized
ui-options-hud-theme-classic = Classic
ui-options-vp-stretch = Вписать viewport в окно игры
ui-options-vp-scale = Фиксированный масштаб viewport: x{ $scale }
ui-options-vp-integer-scaling = Предпочитать целочисленное масштабирование (может вызывать чёрные полосы и обрезание)
ui-options-vp-integer-scaling-tooltip = Если эта опция включена - viewport будет масштабироваться целочисленным методом.
                                        Это приводит к более чётким текстурам, но при этом могут появляться чёрные полосы
                                        сверху/снизу экрана.

ui-options-vp-low-res = Viewport с низким разрешением
ui-options-parallax-low-quality = Низко-качественный параллакс
ui-options-fps-counter = Показывать счётчик FPS
ui-options-film-grain = Эффект зернистостой плёнки
ui-options-shaders = Шейдеры
ui-options-vp-width = Ширина viewport: { $width }
ui-options-hud-layout = Раскладка HUD:

## Controls menu

ui-options-binds-reset-all = Сбросить ВСЕ привязки
ui-options-binds-explanation = Нажмите для изменения привязки, правое нажатие - очистить
ui-options-unbound = Отвязано
ui-options-bind-reset = Сбросить
ui-options-key-prompt = Нажмите на кнопку...

ui-options-header-movement = Передвижение
ui-options-header-camera = Камера
ui-options-header-interaction-basic = Общие взаимодействия
ui-options-header-interaction-adv = Расширенные взаимодействия
ui-options-header-ui = Пользовательский интерфейс
ui-options-header-misc = Прочее
ui-options-header-hotbar = Панель действий
ui-options-header-shuttle = Челнок
ui-options-header-map-editor = Редактор карт
ui-options-header-dev = Разработка
ui-options-header-general = Общее

ui-options-hotkey-keymap = Использовать US QWERTY клавиатуру

ui-options-function-move-up = Движение вверх
ui-options-function-move-left = Движение влево
ui-options-function-move-down = Движение вниз
ui-options-function-move-right = Движение вправо
ui-options-function-walk = Шаг

ui-options-function-camera-rotate-left = Повернуть влево
ui-options-function-camera-rotate-right = Повернуть вправо
ui-options-function-camera-reset = Сбросить

ui-options-function-use = Использовать
ui-options-function-use-secondary = Альт. использование
ui-options-function-activate-item-in-hand = Активация предмета в руке
ui-options-function-alt-activate-item-in-hand = Альт. активация предмета в руке
ui-options-function-activate-item-in-world = Активация предмета вне рук
ui-options-function-alt-activate-item-in-world = Альт. активация предмета вне рук
ui-options-function-drop = Выкинуть предмет
ui-options-function-examine-entity = Осмотреть
ui-options-function-swap-hands = Переключить руки

ui-options-function-smart-equip-backpack = Поместить в рюкзак
ui-options-function-smart-equip-belt = Поместить на ремень
ui-options-function-throw-item-in-hand = Кинуть предмет
ui-options-function-try-pull-object = Тащить предмет
ui-options-function-move-pulled-object = Передвинуть тянущий предмет
ui-options-function-release-pulled-object = Перестать тянуть предмет
ui-options-function-point = Указать на точку

ui-options-function-focus-chat-input-window = Фокус на чат
ui-options-function-focus-local-chat-window = Фокус на чат (IC)
ui-options-function-focus-whisper-chat-window = Фокус на чат (Шёпот)
ui-options-function-focus-radio-window = Фокус на чат (Радио)
ui-options-function-focus-ooc-window = Фокус на чат (OOC)
ui-options-function-focus-admin-chat-window = Фокус на чат (Админ)
ui-options-function-focus-dead-chat-window = Фокус на чат (Мёртв)
ui-options-function-focus-console-chat-window = Фокус на чат (Консоль)
ui-options-function-cycle-chat-channel-forward = Переключить канал (Вперёд)
ui-options-function-cycle-chat-channel-backward = Переключить канал (Назад)
ui-options-function-open-character-menu = Открыть меню персонажа
ui-options-function-open-context-menu = Открыть контекстное меню
ui-options-function-open-crafting-menu = Открыть меню крафта
ui-options-function-open-inventory-menu = Открыть инвентарь
ui-options-function-open-ahelp = Открыть AHelp
ui-options-function-open-abilities-menu = Открыть меню действий
ui-options-function-open-entity-spawn-window = Открыть меню спавна предметов
ui-options-function-open-sandbox-window = Открыть меню песочницы
ui-options-function-open-tile-spawn-window = Открыть меню спавна тайлов
ui-options-function-open-decal-spawn-window = Открыть меню спавна деколей
ui-options-function-open-admin-menu = Открыть админ-меню

ui-options-function-take-screenshot = Сделать снимок экрана
ui-options-function-take-screenshot-no-ui = Сделать снимок экрана (без UI)

ui-options-function-editor-place-object = Положить объект
ui-options-function-editor-cancel-place = Отменить расстановку
ui-options-function-editor-grid-place = Положить прямоугольником
ui-options-function-editor-line-place = Положить в линию
ui-options-function-editor-rotate-object = Вращать
ui-options-function-editor-copy-object = Копировать

ui-options-function-show-debug-console = Открыть консоль
ui-options-function-show-debug-monitors = Показать отладочную информацию
ui-options-function-hide-ui = Скрыть UI

ui-options-function-hotbar1 = Слот быстрой панели 1
ui-options-function-hotbar2 = Слот быстрой панели 2
ui-options-function-hotbar3 = Слот быстрой панели 3
ui-options-function-hotbar4 = Слот быстрой панели 4
ui-options-function-hotbar5 = Слот быстрой панели 5
ui-options-function-hotbar6 = Слот быстрой панели 6
ui-options-function-hotbar7 = Слот быстрой панели 7
ui-options-function-hotbar8 = Слот быстрой панели 8
ui-options-function-hotbar9 = Слот быстрой панели 9
ui-options-function-hotbar0 = Слот быстрой панели 0
ui-options-function-loadout1 = Раскладка быстрой панели 1
ui-options-function-loadout2 = Раскладка быстрой панели 2
ui-options-function-loadout3 = Раскладка быстрой панели 3
ui-options-function-loadout4 = Раскладка быстрой панели 4
ui-options-function-loadout5 = Раскладка быстрой панели 5
ui-options-function-loadout6 = Раскладка быстрой панели 6
ui-options-function-loadout7 = Раскладка быстрой панели 7
ui-options-function-loadout8 = Раскладка быстрой панели 8
ui-options-function-loadout9 = Раскладка быстрой панели 9
ui-options-function-loadout0 = Раскладка быстрой панели 0

ui-options-function-shuttle-strafe-up = Вперёд
ui-options-function-shuttle-strafe-right = Вправо
ui-options-function-shuttle-strafe-left = Влево
ui-options-function-shuttle-strafe-down = Назад
ui-options-function-shuttle-rotate-left = Рысканье влево
ui-options-function-shuttle-rotate-right = Рысканье вправо
ui-options-function-shuttle-brake = Торможение

## Network menu

ui-options-net-predict = Client-side prediction

ui-options-net-interp-ratio = State buffer size
ui-options-net-interp-ratio-tooltip = Increasing this will generally make the game more resistant
                                      to server->client packet-loss, however in doing so it
                                      effectively adds slightly more latency and requires the
                                      client to predict more future ticks.

ui-options-net-predict-tick-bias = Prediction tick bias
ui-options-net-predict-tick-bias-tooltip = Increasing this will generally make the game more resistant
                                           to client->server packet-loss, however in doing so it
                                           effectively adds slightly more latency and requires the
                                           client to predict more future ticks.

ui-options-net-pvs-spawn = PVS entity spawn budget
ui-options-net-pvs-spawn-tooltip = This limits the rate at which the server will send newly spawned
                                       entities to the client. Lowering this can help reduce
                                       stuttering due to entity spawning, but can lead to pop-in.

ui-options-net-pvs-entry = PVS entity budget
ui-options-net-pvs-entry-tooltip = This limits the rate at which the server will send newly visible
                                       entities to the client. Lowering this can help reduce
                                       stuttering, but can lead to pop-in.

ui-options-net-pvs-leave = PVS detach rate
ui-options-net-pvs-leave-tooltip = This limits the rate at which the client will remove
                                       out-of-view entities. Lowering this can help reduce
                                       stuttering when walking around, but could occasionally
                                       lead to mispredicts and other issues.
