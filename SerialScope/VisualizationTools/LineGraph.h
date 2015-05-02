#pragma once

/*
	Feel free to leap headlong down the rabbit hole in trying to understand the way this class works,
	however note that a lot of it is very nitty-gritty C++, and can take the best of us hours to grok.
*/

namespace VisualizationTools
{
	// Model, View, Projection matrix holder datatype
	struct ModelViewProjectionConstantBuffer
	{
		DirectX::XMMATRIX model;
		DirectX::XMMATRIX view;
		DirectX::XMMATRIX projection;
	};

	// Datatype for holding vertex position and color
	struct VertexPositionColor
	{
		DirectX::XMFLOAT3 pos;
		DirectX::XMFLOAT3 color;
	};

	// The C#-visible class that we will deal with
	public ref class LineGraph sealed : Windows::UI::Xaml::Media::Imaging::SurfaceImageSource
	{
	public:

		// Initialize our object with a given width/height in pixels
		LineGraph(int pixelWidth, int pixelHeight);

		// This gets called regularly by the CompositionTarget.Rendering event
		void Render(Platform::Object^ sender, Platform::Object^ e);


		// Set entire array to new data
		void setArray(const Platform::Array<float>^ data);
        void setArray(const Platform::Array<float>^ data, const Platform::Array<float>^ data2);
        void setArray(const Platform::Array<float>^ data, const Platform::Array<float>^ data2, unsigned int iStart, unsigned int iEnd);

		// Get entire internal array stored so far
		Platform::Array<float>^ getArray();

		// Append to array, shifting everything down by one
		void appendToArray(float sample);

		// Set the color of the drawn line
		void setColor(float r, float g, float b);
		void setColor(float r, float g, float b, float r2, float g2, float b2);

		// Set the color of the background
		void setColorBackground(float r, float g, float b, float a);

		// Set the marker index
		void setMarkIndex(int iMarkerIndex);

		// Set Y-limits on what is plotted
		void setYLim(float yMin, float yMax);
        float getYLimMin();
        float getYLimMax();

        // Set the vertical offset
        void setCh1VertOffset(float VertOffset);
        void setCh2VertOffset(float VertOffset);
    
        // Set the channel scale factor
        void setCh1Scale(float fCh1Scale);
        void setCh2Scale(float fCh2Scale);

    private protected:
		void BeginDraw();
		void EndDraw();
		void Clear(Windows::UI::Color color);
		void RenderNextAnimationFrame();

		void OnSuspending(Platform::Object ^sender, Windows::ApplicationModel::SuspendingEventArgs ^e);
		void CreateDeviceResources();

		void updateVertexBuffer();
		void makeConstantBuffers();

		void _makeMarkers();
		int _iMarkerIndex;

        float _fCh1VertOffset;
        float _fCh2VertOffset;

        float _fCh1Scale;
        float _fCh2Scale;

		VertexPositionColor * lineVerts;
		float * data;
		unsigned int N;
		DirectX::XMFLOAT3 color;
		DirectX::XMFLOAT3 color2;
		DirectX::XMFLOAT4 colorBackground;
		bool vbDirty, vbSizeDirty, constantBufferDirty;
		float yMin, yMax;


		// We also need a lock on the buffers when we're drawing!
		HANDLE buffer_lock;
		void lockBuffers();
		void unlockBuffers();


		// Direct3D objects
		Microsoft::WRL::ComPtr<ID3D11Device>                m_d3dDevice;
		Microsoft::WRL::ComPtr<ID3D11DeviceContext>         m_d3dContext;
		Microsoft::WRL::ComPtr<ID3D11RenderTargetView>      m_renderTargetView;
		Microsoft::WRL::ComPtr<ID3D11VertexShader>          m_vertexShader;
		Microsoft::WRL::ComPtr<ID3D11PixelShader>           m_pixelShader;
		Microsoft::WRL::ComPtr<ID3D11InputLayout>           m_inputLayout;
		Microsoft::WRL::ComPtr<ID3D11Buffer>                m_vertexBuffer;
		Microsoft::WRL::ComPtr<ID3D11Buffer>                m_indexBuffer;
		Microsoft::WRL::ComPtr<ID3D11Buffer>                m_constantBuffer;

		ModelViewProjectionConstantBuffer                   m_constantBufferData;

		uint32                                              m_indexCount;

		int                                                 m_width;
		int                                                 m_height;
	};
}