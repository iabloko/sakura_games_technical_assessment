namespace Core.Scripts.Game.GameHelpers
{
    public static class GameConstant
    {
        public const float EPSILON = .001f;

        #region GAME_SCENES

        public const string GAME_INITIALIZATION = "Initialization";
        public const string GAME_PLAY = "Game";
        public const string GAME_CHARACTER_BUILDER = "CharacterBuilder";

        #endregion

        #region ANIMATIONS

        public const string HORIZONTAL = "Horizontal";
        public const string VERTICAL = "Vertical";
        public const string JUMP = "Jump";
        public const string ATTACK = "RightClick";

        #endregion

        #region INPUT_SYSTEM_ACTIONS

        public const string ACTION_SUBMIT = "Submit";
        public const string ACTION_MOVEMENT = "Move";
        public const string ACTION_MOUSE = "Point";
        public const string ACTION_MOUSE_RIGHT_CLICK = "RightClick";

        #endregion

        #region LAYERS

        public const string LAYER_GROUND = "Ground";

        #endregion
        
        #region USER INTERFACE STATE MACHINE VIEWS
        
        public const string GAME_UI_MAIN_VIEW = "GameUIMain/UIStateMachineParentView";
        public const string GAME_UI_CHARACTER_VIEW = "GameUIMain/GameMenuUICharacterView";
        public const string GAME_UI_GAME_PLAY_VIEW = "GameUIMain/GameMenuUIGamePlayView";

        #endregion
    }
}