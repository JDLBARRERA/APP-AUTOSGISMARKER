using UnityEngine;
using UnityEngine.UI;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.UI
{
    public class StoreUIManager : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private StoreDataManager _storeData;

        [Header("Paneles de Interfaz")]
        [SerializeField] private GameObject _storePanel;
        [SerializeField] private GameObject _productPrefab;
        [SerializeField] private Transform _catalogGridParent;

        [Header("Controles")]
        [SerializeField] private Button _openStoreButton;
        [SerializeField] private Button _closeStoreButton;

        private void OnEnable()
        {
            if (_openStoreButton != null) _openStoreButton.onClick.AddListener(OpenStore);
            if (_closeStoreButton != null) _closeStoreButton.onClick.AddListener(CloseStore);
        }

        private void OnDisable()
        {
            if (_openStoreButton != null) _openStoreButton.onClick.RemoveListener(OpenStore);
            if (_closeStoreButton != null) _closeStoreButton.onClick.RemoveListener(CloseStore);
        }

        private void Start()
        {
            if (_storePanel != null) _storePanel.SetActive(false);
            PopulateStorefront();
        }

        private void OpenStore()
        {
            if (_storePanel != null) _storePanel.SetActive(true);
        }

        private void CloseStore()
        {
            if (_storePanel != null) _storePanel.SetActive(false);
        }

        private void PopulateStorefront()
        {
            if (_storeData == null || _productPrefab == null || _catalogGridParent == null)
            {
                Debug.LogWarning("[StoreUI] Faltan referencias asignadas en el Inspector.");
                return;
            }

            foreach (Transform child in _catalogGridParent)
            {
                Destroy(child.gameObject);
            }

            foreach (var product in _storeData.GetAllProducts())
            {
                GameObject productUI = Instantiate(_productPrefab, _catalogGridParent);

                // Búsqueda defensiva en componentes hijos
                Text nameText = FindComponentByName<Text>(productUI, "NameText");
                Text priceText = FindComponentByName<Text>(productUI, "PriceText");
                Button buyButton = FindComponentByName<Button>(productUI, "BuyButton");

                if (nameText != null) nameText.text = product.DisplayName;
                if (priceText != null) priceText.text = $"${product.Price:0.00}";

                if (buyButton != null)
                {
                    string targetUrl = product.PurchaseURL;
                    buyButton.onClick.AddListener(() => GoToCheckout(targetUrl));
                }
            }
        }

        private T FindComponentByName<T>(GameObject parent, string childName) where T : Component
        {
            Transform child = parent.transform.Find(childName);
            if (child == null)
            {
                Debug.LogError($"[StoreUI] No se encontró el objeto hijo '{childName}' en el prefab de producto.");
                return null;
            }
            return child.GetComponent<T>();
        }

        private void GoToCheckout(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }
    }
}
