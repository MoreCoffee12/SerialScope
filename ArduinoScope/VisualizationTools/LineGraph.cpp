//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

#include "pch.h"
#include "LineGraph.h"
#include "BasicReaderWriter.h"

using namespace Platform;
using namespace Microsoft::WRL;
using namespace Windows::ApplicationModel;
using namespace Windows::UI;
using namespace Windows::UI::Xaml;
using namespace DirectX;
using namespace VisualizationTools;


// Helper function for throwing platform exceptions when we have a failed DX call
void ThrowIfFailed(HRESULT hr)
{
	if (FAILED(hr))
	{
		throw Platform::Exception::CreateException(hr);
	}
}


LineGraph::LineGraph(int pixelWidth, int pixelHeight) :
SurfaceImageSource(pixelWidth, pixelHeight, true)
{
	m_width = pixelWidth;
	m_height = pixelHeight;
	lineVerts = NULL;
	N = 0;
	yMin = -1.0f;
	yMax = 1.0f;
	color = XMFLOAT3(1.0f, 1.0f, 1.0f);
	colorBackground = XMFLOAT4(0.0f, 0.0f, 0.0f, 0.000f);
	buffer_lock = CreateMutexEx(NULL, FALSE, NULL, MUTEX_ALL_ACCESS);

	CreateDeviceResources();

	Application::Current->Suspending += ref new SuspendingEventHandler(this, &LineGraph::OnSuspending);
}

// Initialize hardware-dependent resources.
void LineGraph::CreateDeviceResources()
{
	// This flag adds support for surfaces with a different color channel ordering
	// than the API default.
	UINT creationFlags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;

#if defined(_DEBUG)    
	// If the project is in a debug build, enable debugging via SDK Layers.
	creationFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

	// This array defines the set of DirectX hardware feature levels this app will support.
	// Note the ordering should be preserved.
	// Don't forget to declare your application's minimum required feature level in its
	// description.  All applications are assumed to support 9.1 unless otherwise stated.
	const D3D_FEATURE_LEVEL featureLevels[] =
	{
		D3D_FEATURE_LEVEL_11_1,
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0,
		D3D_FEATURE_LEVEL_9_3,
		D3D_FEATURE_LEVEL_9_2,
		D3D_FEATURE_LEVEL_9_1,
	};

	// Create the DX11 API device object, and get a corresponding context.
	ThrowIfFailed(
		D3D11CreateDevice(
		nullptr,                        // Specify nullptr to use the default adapter.
		D3D_DRIVER_TYPE_HARDWARE,
		nullptr,
		creationFlags,                  // Set debug and Direct2D compatibility flags.
		featureLevels,                  // List of feature levels this app can support.
		ARRAYSIZE(featureLevels),
		D3D11_SDK_VERSION,              // Always set this to D3D11_SDK_VERSION for Windows Store apps.
		&m_d3dDevice,                   // Returns the Direct3D device created.
		nullptr,
		&m_d3dContext                   // Returns the device immediate context.
		)
		);

	// Get the Direct3D 11.1 API device.
	ComPtr<IDXGIDevice> dxgiDevice;
	ThrowIfFailed(
		m_d3dDevice.As(&dxgiDevice)
		);

	// Query for ISurfaceImageSourceNative interface.
	Microsoft::WRL::ComPtr<ISurfaceImageSourceNative> sisNative;
	ThrowIfFailed(
		reinterpret_cast<IUnknown*>(this)->QueryInterface(IID_PPV_ARGS(&sisNative))
		);

	// Associate the DXGI device with the SurfaceImageSource.
	ThrowIfFailed(
		sisNative->SetDevice(dxgiDevice.Get())
		);

	BasicReaderWriter^ reader = ref new BasicReaderWriter();

	// Load the vertex shader.
	auto vsBytecode = reader->ReadData("VisualizationTools\\SimpleVertexShader.cso");
	ThrowIfFailed(
		m_d3dDevice->CreateVertexShader(
		vsBytecode->Data,
		vsBytecode->Length,
		nullptr,
		&m_vertexShader
		)
		);

	// Create input layout for vertex shader.
	const D3D11_INPUT_ELEMENT_DESC vertexDesc[] =
	{
		{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "COLOR", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
	};
	ThrowIfFailed(
		m_d3dDevice->CreateInputLayout(
		vertexDesc,
		ARRAYSIZE(vertexDesc),
		vsBytecode->Data,
		vsBytecode->Length,
		&m_inputLayout
		)
		);

	// Load the pixel shader.
	auto psBytecode = reader->ReadData("VisualizationTools\\SimplePixelShader.cso");
	ThrowIfFailed(
		m_d3dDevice->CreatePixelShader(
		psBytecode->Data,
		psBytecode->Length,
		nullptr,
		&m_pixelShader
		)
		);

	// Create the constant buffer.
	const CD3D11_BUFFER_DESC constantBufferDesc = CD3D11_BUFFER_DESC(sizeof(ModelViewProjectionConstantBuffer), D3D11_BIND_CONSTANT_BUFFER);
	ThrowIfFailed(
		m_d3dDevice->CreateBuffer(
		&constantBufferDesc,
		nullptr,
		&m_constantBuffer
		)
		);

	// Initialize constant buffers
	makeConstantBuffers();

}

void LineGraph::lockBuffers() {
	WaitForSingleObjectEx(buffer_lock, INFINITE, false);
}

void LineGraph::unlockBuffers() {
	ReleaseMutex(buffer_lock);
}

void LineGraph::makeConstantBuffers() {
	m_constantBufferData.projection = XMMatrixTranspose(XMMatrixOrthographicRH(2.0f, yMax - yMin, 0.01f, 100.0f));
	XMVECTOR eye = XMVectorSet(0.0f, (yMin + yMax) / 2.0f, 1.0f, 0.0f);
	XMVECTOR at = XMVectorSet(0.0f, (yMin + yMax) / 2.0f, 0.0f, 0.0f);
	XMVECTOR up = XMVectorSet(0.0f, 1.0f, 0.0f, 0.0f);

	m_constantBufferData.view = XMMatrixTranspose(XMMatrixLookAtRH(eye, at, up));
	m_constantBufferData.model = XMMatrixTranspose(XMMatrixRotationY(0));
	constantBufferDirty = true;
}

void LineGraph::setYLim(float yMin, float yMax) {
	lockBuffers();
	this->yMin = yMin;
	this->yMax = yMax;
	this->makeConstantBuffers();
	unlockBuffers();
}

void LineGraph::setArray(const Platform::Array<float>^ padata) {
	// Recreate lineVerts, if need be
	lockBuffers();
	if (padata->Length != this->N) {
		this->N = padata->Length;
		if (this->lineVerts)
			delete[] this->lineVerts;

		this->lineVerts = new VertexPositionColor[this->N];
		for (unsigned int i = 0; i<this->N; ++i) {
			lineVerts[i].pos.x = i / (this->N / 2.0f) - 1;
			lineVerts[i].pos.z = 0.0f;
			lineVerts[i].color = color;
		}
		this->vbSizeDirty = true;
	}

	// Copy data over
	for (unsigned int i = 0; i<this->N; ++i) {
		float * data = padata->Data;
		// Check for NaN and +- Inf
		if (_isnan(data[i]) || !_finite(data[i])) {
			if (data[i] > 0.0f)
				lineVerts[i].pos.y = yMax*1.1f;
			if (data[i] < 0.0f)
				lineVerts[i].pos.y = yMin*1.1f;
		}
		else {
			lineVerts[i].pos.y = data[i];
		}
	}
	this->vbDirty = true;
	unlockBuffers();
}

Platform::Array<float>^ LineGraph::getArray() {
	Platform::Array<float>^ data = ref new Platform::Array<float>(this->N);
	for (unsigned int i = 0; i<this->N; ++i) {
		data->Data[i] = lineVerts[i].pos.y;
	}
	return data;
}


void LineGraph::updateVertexBuffer() {
	lockBuffers();
	if (vbSizeDirty) {
		D3D11_SUBRESOURCE_DATA vertexBufferData = { 0 };
		vertexBufferData.pSysMem = lineVerts;
		vertexBufferData.SysMemPitch = 0;
		vertexBufferData.SysMemSlicePitch = 0;

		CD3D11_BUFFER_DESC vertexBufferDesc(sizeof(VertexPositionColor)*N, D3D11_BIND_VERTEX_BUFFER, D3D11_USAGE_DYNAMIC, D3D11_CPU_ACCESS_WRITE);
		HRESULT hr = m_d3dDevice->CreateBuffer(&vertexBufferDesc, &vertexBufferData, &m_vertexBuffer);
		ThrowIfFailed(hr);
		vbSizeDirty = false;
	}

	if (vbDirty && m_vertexBuffer) {
		D3D11_MAPPED_SUBRESOURCE mappedResource;
		m_d3dContext->Map(m_vertexBuffer.Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &mappedResource);
		memcpy(mappedResource.pData, lineVerts, sizeof(VertexPositionColor)*N);
		m_d3dContext->Unmap(m_vertexBuffer.Get(), 0);
		vbDirty = false;
	}
	unlockBuffers();
}

void LineGraph::appendToArray(float sample) {
	if (this->lineVerts) {
		for (unsigned int i = 0; i<N - 1; ++i) {
			this->lineVerts[i].pos.y = this->lineVerts[i + 1].pos.y;
		}
		this->lineVerts[N - 1].pos.y = sample;
		this->vbDirty = true;
	}
}

void LineGraph::setColor(float r, float g, float b) 
{
	this->color = XMFLOAT3(r, g, b);
	if (this->lineVerts) {
		for (unsigned int i = 0; i<N; ++i) {
			lineVerts[i].color = this->color;
		}
	}
	this->vbDirty = true;
}

void LineGraph::setColorBackground(float r, float g, float b, float a)
{
	this->colorBackground = XMFLOAT4(r, g, b, a);
	this->vbDirty = true;
}


void LineGraph::Render(Platform::Object^ sender, Platform::Object^ e)
{
	// If nothing is dirty, don't do anything!
	if (!vbDirty && !constantBufferDirty)
		return;

	BeginDraw();

	const float background[] = { colorBackground.x, colorBackground.y, colorBackground.z, colorBackground.w };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		background
		);

	if (constantBufferDirty) {
		// Throw the constant matrices up onto the GPU only if we need to
		lockBuffers();
		m_d3dContext->UpdateSubresource(m_constantBuffer.Get(), 0, NULL, &m_constantBufferData, 0, 0);
		constantBufferDirty = false;
		unlockBuffers();
	}

	// Only do the rest if we actually have something to draw!
	this->updateVertexBuffer();
	if (!m_vertexBuffer) {
		EndDraw();
		return;
	}

	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		nullptr
		);

	UINT stride = sizeof(VertexPositionColor);
	UINT offset = 0;
	m_d3dContext->IASetVertexBuffers(
		0,
		1,
		m_vertexBuffer.GetAddressOf(),
		&stride,
		&offset
		);

	m_d3dContext->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP);

	m_d3dContext->IASetInputLayout(m_inputLayout.Get());

	m_d3dContext->VSSetShader(
		m_vertexShader.Get(),
		nullptr,
		0
		);

	m_d3dContext->VSSetConstantBuffers(
		0,
		1,
		m_constantBuffer.GetAddressOf()
		);

	m_d3dContext->PSSetShader(
		m_pixelShader.Get(),
		nullptr,
		0
		);

	m_d3dContext->Draw(this->N, 0);
	EndDraw();
}

// Begins drawing.
void LineGraph::BeginDraw()
{
	POINT offset;
	ComPtr<IDXGISurface> surface;

	// Express target area as a native RECT type.
	RECT updateRectNative;
	updateRectNative.left = 0;
	updateRectNative.top = 0;
	updateRectNative.right = m_width;
	updateRectNative.bottom = m_height;

	// Query for ISurfaceImageSourceNative interface.
	Microsoft::WRL::ComPtr<ISurfaceImageSourceNative> sisNative;
	ThrowIfFailed(
		reinterpret_cast<IUnknown*>(this)->QueryInterface(IID_PPV_ARGS(&sisNative))
		);

	// Begin drawing - returns a target surface and an offset to use as the top left origin when drawing.
	HRESULT beginDrawHR = sisNative->BeginDraw(updateRectNative, &surface, &offset);

	if (SUCCEEDED(beginDrawHR))
	{
		// QI for target texture from DXGI surface.
		ComPtr<ID3D11Texture2D> d3DTexture;
		surface.As(&d3DTexture);

		// Create render target view.
		ThrowIfFailed(
			m_d3dDevice->CreateRenderTargetView(d3DTexture.Get(), nullptr, &m_renderTargetView)
			);

		// Set viewport to the target area in the surface, taking into account the offset returned by BeginDraw.
		D3D11_VIEWPORT viewport;
		viewport.TopLeftX = static_cast<float>(offset.x);
		viewport.TopLeftY = static_cast<float>(offset.y);
		viewport.Width = static_cast<float>(m_width);
		viewport.Height = static_cast<float>(m_height);
		viewport.MinDepth = D3D11_MIN_DEPTH;
		viewport.MaxDepth = D3D11_MAX_DEPTH;
		m_d3dContext->RSSetViewports(1, &viewport);
	}
	else if (beginDrawHR == DXGI_ERROR_DEVICE_REMOVED || beginDrawHR == DXGI_ERROR_DEVICE_RESET)
	{
		// If the device has been removed or reset, attempt to recreate it and continue drawing.
		CreateDeviceResources();
		BeginDraw();
	}
	else
	{
		// Notify the caller by throwing an exception if any other error was encountered.
		ThrowIfFailed(beginDrawHR);
	}
}

// Ends drawing updates started by a previous BeginDraw call.
void LineGraph::EndDraw()
{
	// Query for ISurfaceImageSourceNative interface.
	Microsoft::WRL::ComPtr<ISurfaceImageSourceNative> sisNative;
	ThrowIfFailed(
		reinterpret_cast<IUnknown*>(this)->QueryInterface(IID_PPV_ARGS(&sisNative))
		);

	ThrowIfFailed(
		sisNative->EndDraw()
		);
}

void LineGraph::OnSuspending(Object ^sender, SuspendingEventArgs ^e)
{
	ComPtr<IDXGIDevice3> dxgiDevice;
	m_d3dDevice.As(&dxgiDevice);

	// Hints to the driver that the app is entering an idle state and that its memory can be used temporarily for other apps.
	dxgiDevice->Trim();
}
