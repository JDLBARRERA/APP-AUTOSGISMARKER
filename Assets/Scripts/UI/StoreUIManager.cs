using UnityEngine;
using UnityEngine.UI;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Gestiona la visualización del escaparate y el proceso de redirigir 
    /// al usuario a la pasarela de pago web.
    /// </summary>
    public class StoreUIManager : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private StoreDataManager _storeData;

        [Header("Paneles de Interfaz")]
        [SerializeField] private GameObject _storePanel;
        [SerializeField] private GameObject _productPrefab; // El "cuadrito" del producto en la tienda
        [SerializeField] private Transform _catalogGridParent; // Donde se instancian los productos

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
            _storePanel.SetActive(false);
            PopulateStorefront();
        }

        private void OpenStore()
        {
            _storePanel.SetActive(true);
        }

        private void CloseStore()
        {
            _storePanel.SetActive(false);
        }

        private void PopulateStorefront()
        {
            // Limpiar catálogo previo
            foreach (Transform child in _catalogGridParent)
            {
                Destroy(child.gameObject);
            }

            // Crear los botones de compra para cada producto
            foreach (var product in _storeData.GetAllProducts())
            {
                GameObject productUI = Instantiate(_productPrefab, _catalogGridParent);
                
                // Aquí buscaríamos los componentes de texto e imagen del prefab
                // (Asumiendo que el prefab tiene un script 'ProductCardUI' o lo hacemos directo)
                Text nameText = productUI.transform.Find("NameText").GetComponent<Text>();
                Text priceText = productUI.transform.Find("PriceText").GetComponent<Text>();
                Button buyButton = productUI.transform.Find("BuyButton").GetComponent<Button>();

                nameText.text = product.DisplayName;
                priceText.text = $"${product.Price:0.00}";

                // Asignar la acción de compra
                buyButton.onClick.AddListener(() => GoToCheckout(product.PurchaseURL));
            }
        }

        private void GoToCheckout(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Debug.Log($"[Store] Redirigiendo al navegador seguro: {url}");
                Application.OpenURL(url); // Abre Safari/Chrome para pagar
            }
            else
            {
                Debug.LogWarning("[Store] Este producto no tiene un enlace de compra configurado.");
            }
        }
    }
}
