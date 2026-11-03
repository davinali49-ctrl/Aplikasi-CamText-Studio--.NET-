Imports AForge.Video
Imports AForge.Video.DirectShow
Imports System.Drawing
Imports System.Windows.Forms

Public Class Form1
    Dim videoDevices As FilterInfoCollection
    Dim finalVideo As VideoCaptureDevice
    Dim capturedImage As Bitmap
    Dim currentYPos As Integer = 10 ' Posisi awal teks dari atas (Y)

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Deteksi webcam yang terhubung ke PC/Laptop
        Try
            videoDevices = New FilterInfoCollection(FilterCategory.VideoInputDevice)
            For Each device As FilterInfo In videoDevices
                ComboBox1.Items.Add(device.Name)
            Next
            If ComboBox1.Items.Count > 0 Then ComboBox1.SelectedIndex = 0
        Catch ex As Exception
            MessageBox.Show("Gagal mendeteksi webcam: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        ' Hubungkan Event Click ke Tombol
        AddHandler btnStart.Click, AddressOf btnStart_Click
        AddHandler btnCapture.Click, AddressOf btnCapture_Click
        AddHandler btnAddText.Click, AddressOf btnAddText_Click

        ' Pastikan btnSave terdaftar dan paksa tampil di paling depan (Biar tidak tertutup Panel)
        If Controls.ContainsKey("btnSave") Then
            Dim btn As Control = Controls("btnSave")
            btn.BringToFront()
            AddHandler btn.Click, AddressOf btnSave_Click
        End If
    End Sub

    ' 1. Mulai Kamera (btnStart)
    Private Sub btnStart_Click(sender As Object, e As EventArgs)
        If videoDevices IsNot Nothing AndAlso videoDevices.Count > 0 Then
            ' Hentikan video lama jika sedang berjalan
            If finalVideo IsNot Nothing AndAlso finalVideo.IsRunning Then
                finalVideo.SignalToStop()
            End If

            finalVideo = New VideoCaptureDevice(videoDevices(ComboBox1.SelectedIndex).MonikerString)
            AddHandler finalVideo.NewFrame, AddressOf NewFrame_Event
            finalVideo.Start()
        End If
    End Sub

    ' Frame dari kamera ditampilkan di PictureBox
    Private Sub NewFrame_Event(sender As Object, eventArgs As NewFrameEventArgs)
        Dim bmp As Bitmap = CType(eventArgs.Frame.Clone(), Bitmap)

        ' Membalik gambar secara horizontal agar tidak terbalik/kemirror
        bmp.RotateFlip(RotateFlipType.RotateNoneFlipX)

        PictureBox1.Image = bmp
    End Sub

    ' 2. Ambil Foto dari Webcam (btnCapture)
    Private Sub btnCapture_Click(sender As Object, e As EventArgs)
        If finalVideo IsNot Nothing AndAlso finalVideo.IsRunning Then
            ' Hentikan stream kamera
            finalVideo.SignalToStop()

            ' Lock frame saat ini
            If PictureBox1.Image IsNot Nothing Then
                capturedImage = New Bitmap(PictureBox1.Image)
                PictureBox1.Image = capturedImage
                currentYPos = 10 ' Reset posisi Y ke atas
            End If
        End If
    End Sub

    ' 3. Menambahkan Teks ke Atas Gambar (btnAddText)
    Private Sub btnAddText_Click(sender As Object, e As EventArgs)
        If capturedImage IsNot Nothing AndAlso Not String.IsNullOrEmpty(TextBox1.Text) Then
            Using g As Graphics = Graphics.FromImage(capturedImage)
                ' Font ukuran 75 & bersih (Smooth text)
                Dim fontText As New Font("Segoe UI", 75, FontStyle.Bold)
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias

                Dim textToDraw As String = TextBox1.Text
                Dim xPos As Single = 30
                Dim yPos As Single = currentYPos

                ' 1. Gambar Efek Bayangan/Outline Hitam
                Dim shadowBrush As New SolidBrush(Color.Black)
                g.DrawString(textToDraw, fontText, shadowBrush, New PointF(xPos + 4, yPos + 4))

                ' 2. Gambar Teks Utama Warna Putih
                Dim textBrush As New SolidBrush(Color.White)
                g.DrawString(textToDraw, fontText, textBrush, New PointF(xPos, yPos))

                ' Jarak baris untuk teks berikutnya
                currentYPos += 110
            End Using

            PictureBox1.Image = capturedImage
            TextBox1.Clear()
        End If
    End Sub

    ' 4. FITUR BARU: Simpan Foto ke PC (btnSave)
    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        If PictureBox1.Image IsNot Nothing AndAlso capturedImage IsNot Nothing Then
            Dim sfd As New SaveFileDialog()
            sfd.Filter = "JPEG Image (*.jpg)|*.jpg|PNG Image (*.png)|*.png"
            sfd.Title = "Simpan Hasil Foto"
            sfd.FileName = "Webcam_Photo_" & DateTime.Now.ToString("yyyyMMdd_HHmmss")

            If sfd.ShowDialog() = DialogResult.OK Then
                Try
                    If sfd.FilterIndex = 1 Then
                        capturedImage.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Jpeg)
                    Else
                        capturedImage.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png)
                    End If
                    MessageBox.Show("Foto berhasil disimpan!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Gagal menyimpan foto: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        Else
            MessageBox.Show("Belum ada foto yang diambil/diolah!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    ' Hentikan kamera saat aplikasi ditutup
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If finalVideo IsNot Nothing AndAlso finalVideo.IsRunning Then
            finalVideo.SignalToStop()
        End If
    End Sub
End Class