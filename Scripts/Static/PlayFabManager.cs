using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
using Photon.Realtime;

public class PlayFabManager : MonoBehaviour
{

    public static PlayFabManager Instance { get { return instance != null ? instance : null; } }
    private static PlayFabManager instance = null;

    private bool isLogin { get { return PlayFabClientAPI.IsClientLoggedIn(); } }

    private Action<bool, string> loginResult, registerResult, recoveryResult;
    private Action<bool, string, string> getAccountInfoResult;

    private string playFabId;

    void Awake()
    {
        if(instance != null)
        {
            if (instance != this)
                Destroy(this.gameObject);
        }
        else
        {
            instance = this;

            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void LoginWithEmail(string email, string password, Action<bool, string> callBack)
    {
        if (isLogin)
            return;

        loginResult = callBack;

        if (!string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            LoginWithEmailAddressRequest request = new LoginWithEmailAddressRequest
            {
                TitleId = PlayFabSettings.TitleId,
                Email = email,
                Password = password,
            };

            PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFail);
        }
    }

    public void LoginWithID(string id, string password, Action<bool, string> callBack)
    {
        if (isLogin)
            return;

        loginResult = callBack;

        if (!string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            LoginWithPlayFabRequest request = new LoginWithPlayFabRequest
            {
                TitleId = PlayFabSettings.TitleId,
                Username = id,
                Password = password,
            };

            PlayFabClientAPI.LoginWithPlayFab(request, OnLoginSuccess, OnLoginFail);
        }
    }

    public void Register(string id, string nickName, string email, string password, Action<bool, string> callBack)
    {
        if (isLogin)
            return;

        registerResult = callBack;

        if (!string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            RegisterPlayFabUserRequest request = new RegisterPlayFabUserRequest
            {
                TitleId = PlayFabSettings.TitleId,
                DisplayName = nickName,
                Username = id,
                Email = email,
                Password = password,
            };

            Debug.Log(PlayFabSettings.TitleId.ToString());

            PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnRegisterFail);
        }
    }

    public void RecoveryAccount(string email, Action<bool, string> callBack)
    {
        if (isLogin)
            return;

        recoveryResult = callBack;

        if (!string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            SendAccountRecoveryEmailRequest request = new SendAccountRecoveryEmailRequest
            {
                TitleId = PlayFabSettings.TitleId,
                Email = email,
            };

            PlayFabClientAPI.SendAccountRecoveryEmail(request, OnRecoverySuccess, OnRecoveryFail);
        }
    }

    public void GetAccountInfoWithEmail(string email, Action<bool, string, string> callBack)
    {
        getAccountInfoResult = callBack;

        if (!string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            GetAccountInfoRequest request = new GetAccountInfoRequest
            {
                Email = email,
            };

            PlayFabClientAPI.GetAccountInfo(request, OnGetAccountSuccess, OnGetAccountFail);
        }
    }

    public void GetAccountInfoWithID(string id, Action<bool, string, string> callBack)
    {
        getAccountInfoResult = callBack;

        if (!string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            GetAccountInfoRequest request = new GetAccountInfoRequest
            {
                Username = id,
            };

            PlayFabClientAPI.GetAccountInfo(request, OnGetAccountSuccess, OnGetAccountFail);
        }
    }

    public void Logout()
    {
        if (isLogin)
        {
            PlayFabClientAPI.ForgetAllCredentials();

            Debug.Log("로그아웃");
        }
    }

    void OnLoginSuccess(LoginResult result)
    {
        loginResult(true, "");
        RequestPhotonToken(result);

        Debug.Log("로그인 성공");
    }

    void OnLoginFail(PlayFabError error)
    {
        loginResult(false, GetErrorMessage(error.ErrorMessage));

        Debug.Log("로그인 실패");
        Debug.Log(error.ErrorMessage);
        Debug.Log(error.HttpCode.ToString());
        Debug.Log(error.HttpStatus);
        Debug.Log(error.ApiEndpoint);

        Debug.Log(error.GenerateErrorReport());
    }

    void RequestPhotonToken(LoginResult result)
    {
        playFabId = result.PlayFabId;

        PlayFabClientAPI.GetPhotonAuthenticationToken(new GetPhotonAuthenticationTokenRequest()
        {
            PhotonApplicationId = Photon.Pun.PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime
        }, AuthenticateWithPhoton, OnPlayFabError);
    }

    void AuthenticateWithPhoton(GetPhotonAuthenticationTokenResult result)
    {
        Debug.Log("Photon token acquired: " + result.PhotonCustomAuthenticationToken + "  Authentication complete.");

        AuthenticationValues customAuth = new AuthenticationValues { AuthType = CustomAuthenticationType.Custom };

        customAuth.AddAuthParameter("username", playFabId);
        customAuth.AddAuthParameter("token", result.PhotonCustomAuthenticationToken);

        PhotonNetwork.AuthValues = customAuth;
    }

    void OnPlayFabError(PlayFabError obj)
    {
        Debug.Log(obj.GenerateErrorReport());
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        registerResult(true, "");

        Debug.Log("회원가입 성공");
    }

    void OnRegisterFail(PlayFabError error)
    {
        registerResult(false, GetErrorMessage(error.ErrorMessage));

        Debug.Log("회원가입 실패");
        Debug.Log(error.ErrorMessage);
        Debug.Log(error.HttpCode.ToString());
        Debug.Log(error.HttpStatus);
        Debug.Log(error.ApiEndpoint);

        Debug.Log(error.GenerateErrorReport());
    }

    public void OnRecoverySuccess(SendAccountRecoveryEmailResult result)
    {
        recoveryResult(true, "");

        Debug.Log("이메일 전송 성공");
    }

    public void OnRecoveryFail(PlayFabError error)
    {
        recoveryResult(false, GetErrorMessage(error.ErrorMessage));

        Debug.Log("이메일 전송 실패");
        Debug.Log(error.ErrorMessage);
        Debug.Log(error.HttpCode.ToString());
        Debug.Log(error.HttpStatus);
        Debug.Log(error.ApiEndpoint);

        Debug.Log(error.GenerateErrorReport());
    }

    void OnGetAccountSuccess(GetAccountInfoResult result)
    {
        getAccountInfoResult(true, "", result.AccountInfo.TitleInfo.DisplayName);

        Debug.Log("계정 정보 가져오기 성공");
    }

    void OnGetAccountFail(PlayFabError error)
    {
        getAccountInfoResult(false, GetErrorMessage(error.ErrorMessage), "");

        Debug.Log("계정 정보 가져오기 실패");
        Debug.Log(error.ErrorMessage);
        Debug.Log(error.HttpCode.ToString());
        Debug.Log(error.HttpStatus);
        Debug.Log(error.ApiEndpoint);

        Debug.Log(error.GenerateErrorReport());
    }

    string GetErrorMessage(string message)
    {
        switch (message)
        {
            case "Invalid input parameters":
                message = "잘못된 입력 값입니다.";
                break;
            case "Invalid email address":
                message = "유효하지 않은 이메일 주소입니다.";
                break;
            case "The account making this request is currently banned":
                message = "이용이 제한된 계정입니다.";
                break;
            case "The email PlayFab.DataModel.EmailBlacklist was blacklisted by PlayFab for previously bouncing":
                message = "최근 이용이 제한되었던 계정의 이메일입니다.";
                break;
            case "Email address not available":
                message = "이미 사용 중이거나 올바르지 않은 이메일입니다.";
                break;
            case "Username not available":
                message = "이미 존재하거나 금지된 아이디입니다.";
                break;
            case "The display name entered is not available.":
                message = "이미 존재하거나 금지된 닉네임입니다.";
                break;
            case "User not found":
                message = "아이디 또는 비밀번호가 일치하지 않습니다.";
                break;
            case "Invalid username or password":
                message = "아이디 또는 비밀번호가 일치하지 않습니다.";
                break;
            default:
                break;
        }

        return message;
    }

    void OnApplicationQuit()
    {
        Logout();
    }
}
