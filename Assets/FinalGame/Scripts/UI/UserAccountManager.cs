using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UserAccountManager : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    public void Register()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        if (!PlayerPrefs.HasKey(username))
        {
            if (username == "" || password == "")
            {
                PlayerPrefs.SetString(username, password);
                Debug.Log("Wrong format.");
            }
            else {
                PlayerPrefs.SetString(username, password);
                Debug.Log("User registered successfully.");
            }
        }
        else
        {
            Debug.Log("Username already exists.");
        }
    }

    public void Login()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        if (PlayerPrefs.HasKey(username))
        {
            if (PlayerPrefs.GetString(username) == password)
            {
                Debug.Log("Login successful.");
                SceneManager.LoadScene(1);
                // Chuyển đến màn hình tiếp theo
            }
            else
            {
                Debug.Log("Incorrect password.");
            }
        }
        else
        {
            Debug.Log("Username not found.");
        }
    }
    public void Quit()
    {
        Application.Quit();
        EditorApplication.isPlaying = false;

    }
}
